import React, { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import './VideoChat.css';
import api from '../services/api';

interface VideoChatProps {
  sessionId: string;
  userId: string;
  peerUserId?: string;
  onError?: (error: string) => void;
  showLocalVideo?: boolean; // Show local video as main video
  showLocalPreview?: boolean; // Option to show small local video preview (for floating windows)
  overlayControls?: boolean; // Option to overlay controls on video instead of below
}

export const VideoChat: React.FC<VideoChatProps> = ({
  sessionId,
  userId,
  peerUserId: _peerUserId, // Reserved for future use (e.g., peer identification)
  onError,
  showLocalVideo = true, // Default to showing local video
  showLocalPreview = false, // Default to no local preview
  overlayControls = false, // Default to controls below video
}) => {
  const localVideoRef = useRef<HTMLVideoElement>(null);
  const remoteVideoRef = useRef<HTMLVideoElement>(null);
  // Load saved state from localStorage
  const getSavedVideoState = (): boolean => {
    const saved = localStorage.getItem(`video_enabled_${sessionId}`);
    return saved === null ? true : saved === 'true';
  };

  const getSavedAudioState = (): boolean => {
    const saved = localStorage.getItem(`audio_enabled_${sessionId}`);
    return saved === null ? true : saved === 'true';
  };

  const [isVideoEnabled, setIsVideoEnabled] = useState(getSavedVideoState);
  const [isAudioEnabled, setIsAudioEnabled] = useState(getSavedAudioState);
  const [isScreenSharing, setIsScreenSharing] = useState(false);
  const [localStream, setLocalStream] = useState<MediaStream | null>(null);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
  const peerConnectionRef = useRef<RTCPeerConnection | null>(null);
  const signalRConnectionRef = useRef<signalR.HubConnection | null>(null);
  const isOfferSentRef = useRef<boolean>(false);
  const screenStreamRef = useRef<MediaStream | null>(null);

  useEffect(() => {
    initializeVideoAndSignaling();
    return () => {
      cleanup();
    };
  }, [sessionId]);

  const initializeSignalR = async (): Promise<signalR.HubConnection> => {
    // Get access token for authentication
    const accessToken = localStorage.getItem('accessToken');
    if (!accessToken) {
      throw new Error('Authentication required');
    }

    // Get base URL without /api suffix
    const baseUrl = (api.defaults.baseURL && typeof api.defaults.baseURL === 'string') 
      ? api.defaults.baseURL.replace('/api', '') 
      : 'http://localhost:5000';
    
    // Create SignalR connection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/api/collaboration?access_token=${accessToken}`, {
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount < 3) {
            return 2000; // 2 seconds
          } else if (retryContext.previousRetryCount < 10) {
            return 5000; // 5 seconds
          } else {
            return 10000; // 10 seconds
          }
        },
      })
      .build();

    // Set up WebRTC signaling event handlers
    connection.on('WebRTCOffer', async (data: { userId: string; offer: string }) => {
      if (data.userId !== userId && peerConnectionRef.current) {
        await handleOffer(data.offer);
      }
    });

    connection.on('WebRTCAnswer', async (data: { userId: string; answer: string }) => {
      if (data.userId !== userId && peerConnectionRef.current) {
        await handleAnswer(data.answer);
      }
    });

    connection.on('WebRTCIceCandidate', async (data: { userId: string; candidate: string; sdpMLineIndex?: number; sdpMid?: string }) => {
      if (data.userId !== userId && peerConnectionRef.current) {
        await handleIceCandidate(data.candidate);
      }
    });

    // Start connection
    await connection.start();
    
    // Join the session group
    await connection.invoke('JoinSession', sessionId);

    return connection;
  };

  const initializeVideoAndSignaling = async () => {
    try {
      // Initialize SignalR connection first
      const connection = await initializeSignalR();
      signalRConnectionRef.current = connection;

      // Get saved video/audio states
      const videoEnabled = getSavedVideoState();
      const audioEnabled = getSavedAudioState();

      // Get local media stream with saved preferences
      const stream = await navigator.mediaDevices.getUserMedia({
        video: videoEnabled ? {
          width: { ideal: 1280 },
          height: { ideal: 720 },
          facingMode: 'user'
        } : false,
        audio: audioEnabled ? {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true
        } : false,
      });

      if (localVideoRef.current) {
        localVideoRef.current.srcObject = stream;
      }

      setLocalStream(stream);
      
      // Apply saved states to tracks
      if (!videoEnabled && stream.getVideoTracks().length > 0) {
        stream.getVideoTracks()[0].enabled = false;
      }
      if (!audioEnabled && stream.getAudioTracks().length > 0) {
        stream.getAudioTracks()[0].enabled = false;
      }

      // Initialize WebRTC peer connection
      const configuration = {
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
          { urls: 'stun:stun1.l.google.com:19302' },
          // Add TURN servers if needed for NAT traversal (configure via appsettings)
        ],
      };

      const peerConnection = new RTCPeerConnection(configuration);
      peerConnectionRef.current = peerConnection;

      // Add local stream tracks to peer connection
      stream.getTracks().forEach((track) => {
        peerConnection.addTrack(track, stream);
      });

      // Handle remote stream
      peerConnection.ontrack = (event) => {
        if (remoteVideoRef.current) {
          remoteVideoRef.current.srcObject = event.streams[0];
          setRemoteStream(event.streams[0]);
        }
      };

      // Handle ICE candidates
      peerConnection.onicecandidate = async (event) => {
        if (event.candidate && signalRConnectionRef.current && sessionId) {
          try {
            await signalRConnectionRef.current.invoke('SendWebRTCIceCandidate', 
              sessionId,
              JSON.stringify(event.candidate),
              event.candidate.sdpMLineIndex ?? null,
              event.candidate.sdpMid ?? null
            );
          } catch (error) {
            console.error('Failed to send ICE candidate:', error);
          }
        }
      };

      // Handle connection state changes
      peerConnection.onconnectionstatechange = () => {
        if (peerConnection.connectionState === 'failed') {
          onError?.('WebRTC connection failed. Please try refreshing.');
        } else if (peerConnection.connectionState === 'connected') {
          console.log('WebRTC connection established');
        }
      };

      // Handle ICE connection state changes
      peerConnection.oniceconnectionstatechange = () => {
        if (peerConnection.iceConnectionState === 'failed') {
          // Try ICE restart
          peerConnection.restartIce();
        }
      };

      // Create and send offer if we're first to join
      // Wait a bit to see if another user sends an offer first
      setTimeout(async () => {
        if (!isOfferSentRef.current && peerConnectionRef.current && signalRConnectionRef.current) {
          await createAndSendOffer();
        }
      }, 1000); // Wait 1 second to allow peer to potentially send offer first

    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to access camera/microphone';
      onError?.(errorMessage);
      console.error('Error initializing video:', error);
    }
  };

  const createAndSendOffer = async () => {
    try {
      if (!peerConnectionRef.current || !signalRConnectionRef.current || !sessionId) return;

      const offer = await peerConnectionRef.current.createOffer({
        offerToReceiveAudio: true,
        offerToReceiveVideo: true,
      });
      await peerConnectionRef.current.setLocalDescription(offer);
      
      await signalRConnectionRef.current.invoke('SendWebRTCOffer', sessionId, JSON.stringify(offer));
      isOfferSentRef.current = true;
    } catch (error) {
      console.error('Failed to create and send offer:', error);
      onError?.('Failed to start video call');
    }
  };

  const handleOffer = async (offerString: string) => {
    try {
      if (!peerConnectionRef.current || !signalRConnectionRef.current || !sessionId) return;

      const offer = JSON.parse(offerString);
      await peerConnectionRef.current.setRemoteDescription(new RTCSessionDescription(offer));

      // Create and send answer
      const answer = await peerConnectionRef.current.createAnswer({
        offerToReceiveAudio: true,
        offerToReceiveVideo: true,
      });
      await peerConnectionRef.current.setLocalDescription(answer);
      
      await signalRConnectionRef.current.invoke('SendWebRTCAnswer', sessionId, JSON.stringify(answer));
      isOfferSentRef.current = true;
    } catch (error) {
      console.error('Failed to handle offer:', error);
      onError?.('Failed to establish video connection');
    }
  };

  const handleAnswer = async (answerString: string) => {
    try {
      if (!peerConnectionRef.current) return;

      const answer = JSON.parse(answerString);
      await peerConnectionRef.current.setRemoteDescription(new RTCSessionDescription(answer));
    } catch (error) {
      console.error('Failed to handle answer:', error);
      onError?.('Failed to complete video connection');
    }
  };

  const handleIceCandidate = async (candidateString: string) => {
    try {
      if (!peerConnectionRef.current) return;

      const candidate = JSON.parse(candidateString);
      await peerConnectionRef.current.addIceCandidate(new RTCIceCandidate(candidate));
    } catch (error) {
      // Silently fail - candidate might already be added or invalid
      console.debug('Failed to add ICE candidate:', error);
    }
  };

  const cleanup = async () => {
    // Stop all media tracks
    if (localStream) {
      localStream.getTracks().forEach((track) => track.stop());
    }
    if (remoteStream) {
      remoteStream.getTracks().forEach((track) => track.stop());
    }
    if (screenStreamRef.current) {
      screenStreamRef.current.getTracks().forEach((track) => track.stop());
      screenStreamRef.current = null;
    }

    // Close peer connection
    if (peerConnectionRef.current) {
      peerConnectionRef.current.close();
      peerConnectionRef.current = null;
    }

    // Leave SignalR group and stop connection
    if (signalRConnectionRef.current) {
      try {
        if (sessionId) {
          await signalRConnectionRef.current.invoke('LeaveSession', sessionId);
        }
        await signalRConnectionRef.current.stop();
      } catch (error) {
        console.error('Error cleaning up SignalR connection:', error);
      }
      signalRConnectionRef.current = null;
    }

    isOfferSentRef.current = false;
  };

  const toggleVideo = async () => {
    const newState = !isVideoEnabled;
    
    if (localStream) {
      const videoTrack = localStream.getVideoTracks()[0];
      if (videoTrack) {
        if (newState) {
          // Enable video - get new stream with video
          try {
            const stream = await navigator.mediaDevices.getUserMedia({
              video: {
                width: { ideal: 1280 },
                height: { ideal: 720 },
                facingMode: 'user'
              },
              audio: isAudioEnabled ? {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
              } : false,
            });
            
            // Replace video track in peer connection
            const newVideoTrack = stream.getVideoTracks()[0];
            const sender = peerConnectionRef.current?.getSenders()
              .find((s) => s.track && s.track.kind === 'video');
            
            if (sender && newVideoTrack) {
              await sender.replaceTrack(newVideoTrack);
            }
            
            // Replace video track in local stream
            localStream.removeTrack(videoTrack);
            localStream.addTrack(newVideoTrack);
            
            // Update video element
            if (localVideoRef.current) {
              localVideoRef.current.srcObject = stream;
            }
            
            // Stop old track
            videoTrack.stop();
            stream.getVideoTracks().forEach(track => {
              if (track !== newVideoTrack) track.stop();
            });
          } catch (error) {
            console.error('Failed to enable video:', error);
            onError?.('Failed to enable camera');
            return;
          }
        } else {
          // Disable video - stop the track
          videoTrack.enabled = false;
          videoTrack.stop();
          localStream.removeTrack(videoTrack);
          
          // Remove video track from peer connection
          const sender = peerConnectionRef.current?.getSenders()
            .find((s) => s.track && s.track.kind === 'video');
          if (sender) {
            await sender.replaceTrack(null);
          }
        }
      } else if (newState) {
        // No video track exists, create new stream
        try {
          const stream = await navigator.mediaDevices.getUserMedia({
            video: {
              width: { ideal: 1280 },
              height: { ideal: 720 },
              facingMode: 'user'
            },
            audio: isAudioEnabled ? {
              echoCancellation: true,
              noiseSuppression: true,
              autoGainControl: true
            } : false,
          });
          
          if (localVideoRef.current) {
            localVideoRef.current.srcObject = stream;
          }
          
          stream.getTracks().forEach((track) => {
            peerConnectionRef.current?.addTrack(track, stream);
          });
          
          setLocalStream(stream);
        } catch (error) {
          console.error('Failed to enable video:', error);
          onError?.('Failed to enable camera');
          return;
        }
      }
    }
    
    setIsVideoEnabled(newState);
    localStorage.setItem(`video_enabled_${sessionId}`, String(newState));
  };

  const toggleAudio = async () => {
    const newState = !isAudioEnabled;
    
    if (localStream) {
      const audioTrack = localStream.getAudioTracks()[0];
      if (audioTrack) {
        if (newState) {
          // Enable audio - get new stream with audio
          try {
            const stream = await navigator.mediaDevices.getUserMedia({
              video: isVideoEnabled ? {
                width: { ideal: 1280 },
                height: { ideal: 720 },
                facingMode: 'user'
              } : false,
              audio: {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
              },
            });
            
            // Replace audio track in peer connection
            const newAudioTrack = stream.getAudioTracks()[0];
            const sender = peerConnectionRef.current?.getSenders()
              .find((s) => s.track && s.track.kind === 'audio');
            
            if (sender && newAudioTrack) {
              await sender.replaceTrack(newAudioTrack);
            }
            
            // Replace audio track in local stream
            localStream.removeTrack(audioTrack);
            localStream.addTrack(newAudioTrack);
            
            // Stop old track
            audioTrack.stop();
            stream.getAudioTracks().forEach(track => {
              if (track !== newAudioTrack) track.stop();
            });
            stream.getVideoTracks().forEach(track => track.stop());
          } catch (error) {
            console.error('Failed to enable audio:', error);
            onError?.('Failed to enable microphone');
            return;
          }
        } else {
          // Disable audio - mute the track
          audioTrack.enabled = false;
        }
      } else if (newState) {
        // No audio track exists, create new stream
        try {
          const stream = await navigator.mediaDevices.getUserMedia({
            video: isVideoEnabled ? {
              width: { ideal: 1280 },
              height: { ideal: 720 },
              facingMode: 'user'
            } : false,
            audio: {
              echoCancellation: true,
              noiseSuppression: true,
              autoGainControl: true
            },
          });
          
          stream.getTracks().forEach((track) => {
            peerConnectionRef.current?.addTrack(track, stream);
          });
          
          setLocalStream(stream);
        } catch (error) {
          console.error('Failed to enable audio:', error);
          onError?.('Failed to enable microphone');
          return;
        }
      }
    }
    
    setIsAudioEnabled(newState);
    localStorage.setItem(`audio_enabled_${sessionId}`, String(newState));
  };

  const toggleScreenShare = async () => {
    try {
      if (!peerConnectionRef.current || !localStream) {
        onError?.('Video connection not ready');
        return;
      }

      if (!isScreenSharing) {
        const screenStream = await navigator.mediaDevices.getDisplayMedia({
          video: true,
          audio: true,
        } as MediaStreamConstraints);

        screenStreamRef.current = screenStream;

        if (localVideoRef.current) {
          localVideoRef.current.srcObject = screenStream;
        }

        // Replace video track in peer connection
        const videoTrack = screenStream.getVideoTracks()[0];
        const sender = peerConnectionRef.current
          .getSenders()
          .find((s) => s.track && s.track.kind === 'video');
        if (sender && videoTrack) {
          await sender.replaceTrack(videoTrack);
        }

        // Handle screen share ending
        screenStream.getVideoTracks()[0].onended = async () => {
          await toggleScreenShare();
        };

        setIsScreenSharing(true);
      } else {
        // Switch back to camera
        const cameraStream = await navigator.mediaDevices.getUserMedia({
          video: {
            width: { ideal: 1280 },
            height: { ideal: 720 },
            facingMode: 'user'
          },
          audio: {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true
          },
        });

        if (screenStreamRef.current) {
          screenStreamRef.current.getTracks().forEach((track) => track.stop());
          screenStreamRef.current = null;
        }

        if (localVideoRef.current) {
          localVideoRef.current.srcObject = cameraStream;
        }

        // Update local stream state
        setLocalStream(cameraStream);

        // Replace video track in peer connection
        const videoTrack = cameraStream.getVideoTracks()[0];
        const sender = peerConnectionRef.current
          .getSenders()
          .find((s) => s.track && s.track.kind === 'video');
        if (sender && videoTrack) {
          await sender.replaceTrack(videoTrack);
        }

        setIsScreenSharing(false);
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to share screen';
      onError?.(errorMessage);
      console.error('Error toggling screen share:', error);
    }
  };

  return (
    <div className="video-chat-container">
      <div className={`video-chat-grid ${!showLocalVideo ? 'single-video' : ''}`}>
        {showLocalVideo && (
          <div className="video-wrapper">
            <video
              ref={localVideoRef}
              autoPlay
              muted
              playsInline
              className="video-element local-video"
            />
            <div className="video-label">You</div>
            {!isVideoEnabled && (
              <div className="video-overlay">
                <i className="fas fa-video-slash"></i>
              </div>
            )}
          </div>
        )}
        {!showLocalVideo && (
          <div className="video-wrapper">
            <video
              ref={remoteVideoRef}
              autoPlay
              playsInline
              className="video-element remote-video"
            />
            {!remoteStream && (
              <div className="video-overlay">
                <i className="fas fa-user-slash"></i>
                <span>Waiting for partner...</span>
              </div>
            )}
            {showLocalPreview && localStream && (
              <div className="local-video-preview">
                <video
                  ref={localVideoRef}
                  autoPlay
                  muted
                  playsInline
                  className="preview-video-element"
                />
              </div>
            )}
            {overlayControls && (
              <div className="video-controls-overlay">
                <button
                  className={`control-btn ${isVideoEnabled ? 'active' : ''}`}
                  onClick={toggleVideo}
                  title={isVideoEnabled ? 'Turn off camera' : 'Turn on camera'}
                >
                  <i className={`fas ${isVideoEnabled ? 'fa-video' : 'fa-video-slash'}`}></i>
                </button>
                <button
                  className={`control-btn ${isAudioEnabled ? 'active' : ''}`}
                  onClick={toggleAudio}
                  title={isAudioEnabled ? 'Mute microphone' : 'Unmute microphone'}
                >
                  <i className={`fas ${isAudioEnabled ? 'fa-microphone' : 'fa-microphone-slash'}`}></i>
                </button>
              </div>
            )}
          </div>
        )}
      </div>
      {!overlayControls && (
        <div className="video-controls">
          <button
            className={`control-btn ${isVideoEnabled ? 'active' : ''}`}
            onClick={toggleVideo}
            title={isVideoEnabled ? 'Turn off camera' : 'Turn on camera'}
          >
            <i className={`fas ${isVideoEnabled ? 'fa-video' : 'fa-video-slash'}`}></i>
          </button>
          <button
            className={`control-btn ${isAudioEnabled ? 'active' : ''}`}
            onClick={toggleAudio}
            title={isAudioEnabled ? 'Mute microphone' : 'Unmute microphone'}
          >
            <i className={`fas ${isAudioEnabled ? 'fa-microphone' : 'fa-microphone-slash'}`}></i>
          </button>
          <button
            className={`control-btn ${isScreenSharing ? 'active' : ''}`}
            onClick={toggleScreenShare}
            title={isScreenSharing ? 'Stop sharing' : 'Share screen'}
          >
            <i className="fas fa-desktop"></i>
          </button>
        </div>
      )}
    </div>
  );
};
