import React, { useEffect, useImperativeHandle, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import './VideoChat.css';
import api from '../services/api';
import { tokenStorage } from '../utils/tokenStorage';

export type VideoChatState = {
  isVideoEnabled: boolean;
  isAudioEnabled: boolean;
  hasRemoteStream: boolean;
};

export type VideoChatHandle = {
  toggleVideo: () => Promise<void>;
  toggleAudio: () => Promise<void>;
  getState: () => VideoChatState;
};

interface VideoChatProps {
  sessionId: string;
  userId: string;
  peerUserId?: string;
  onError?: (error: string) => void;
  showLocalVideo?: boolean; // Show local video as main video
  showLocalPreview?: boolean; // Option to show small local video preview (for floating windows)
  overlayControls?: boolean; // Option to overlay controls on video instead of below
  hideControls?: boolean; // Allow parent to render custom controls
  onStateChange?: (state: VideoChatState) => void;
}

export const VideoChat = React.forwardRef<VideoChatHandle, VideoChatProps>(({
  sessionId,
  userId,
  peerUserId,
  onError,
  showLocalVideo = true, // Default to showing local video
  showLocalPreview = false, // Default to no local preview
  overlayControls = false, // Default to controls below video
  hideControls = false,
  onStateChange,
}, ref) => {
  const localVideoRef = useRef<HTMLVideoElement>(null);
  const remoteVideoRef = useRef<HTMLVideoElement>(null);
  const localPreviewRef = useRef<HTMLDivElement>(null);
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
  const [hasRemoteVideoTrack, setHasRemoteVideoTrack] = useState(false);
  const [isRemoteVideoMuted, setIsRemoteVideoMuted] = useState(false);
  const [connectionState, setConnectionState] = useState<string>('connecting');
  const [showSettings, setShowSettings] = useState(false);
  const [devices, setDevices] = useState<MediaDeviceInfo[]>([]);
  const [selectedVideoDeviceId, setSelectedVideoDeviceId] = useState<string>('');
  const [selectedAudioDeviceId, setSelectedAudioDeviceId] = useState<string>('');

  const peerConnectionRef = useRef<RTCPeerConnection | null>(null);
  const signalRConnectionRef = useRef<signalR.HubConnection | null>(null);
  const isOfferSentRef = useRef<boolean>(false);
  const screenStreamRef = useRef<MediaStream | null>(null);

  useEffect(() => {
    initializeVideoAndSignaling();
    return () => {
      cleanup();
    };
    // Re-init when peerUserId becomes available so we can deterministically pick an offerer.
  }, [sessionId, peerUserId]);

  // Ensure the local <video> element always receives the latest local stream.
  // In remote-only layouts (showLocalVideo=false), the preview video mounts only
  // after localStream is set, so we must attach srcObject in an effect.
  useEffect(() => {
    if (!localStream) return;
    if (!localVideoRef.current) return;

    try {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (localVideoRef.current as any).srcObject = localStream;
      localVideoRef.current.play?.().catch(() => {});
    } catch {
      // ignore
    }
  }, [localStream]);

  useEffect(() => {
    // Initial fetch of devices
    const getDevices = async () => {
      try {
        const d = await navigator.mediaDevices.enumerateDevices();
        setDevices(d);
        const savedVideoId = localStorage.getItem(`selected_video_device_${sessionId}`);
        const savedAudioId = localStorage.getItem(`selected_audio_device_${sessionId}`);
        
        const hasVideo = d.some(dev => dev.deviceId === savedVideoId && dev.kind === 'videoinput');
        const hasAudio = d.some(dev => dev.deviceId === savedAudioId && dev.kind === 'audioinput');

        if (hasVideo && savedVideoId) setSelectedVideoDeviceId(savedVideoId);
        else {
          const firstVideo = d.find(dev => dev.kind === 'videoinput');
          if (firstVideo) setSelectedVideoDeviceId(firstVideo.deviceId);
        }

        if (hasAudio && savedAudioId) setSelectedAudioDeviceId(savedAudioId);
        else {
          const firstAudio = d.find(dev => dev.kind === 'audioinput');
          if (firstAudio) setSelectedAudioDeviceId(firstAudio.deviceId);
        }
      } catch (err) {
        console.error('Error fetching devices', err);
      }
    };
    getDevices();
    navigator.mediaDevices.addEventListener('devicechange', getDevices);
    return () => {
      navigator.mediaDevices.removeEventListener('devicechange', getDevices);
    };
  }, [sessionId]);

  const changeDevice = async (kind: 'audioinput' | 'videoinput', deviceId: string) => {
    if (kind === 'videoinput') {
      setSelectedVideoDeviceId(deviceId);
      localStorage.setItem(`selected_video_device_${sessionId}`, deviceId);
      if (!isVideoEnabled || !localStream) return;
    } else {
      setSelectedAudioDeviceId(deviceId);
      localStorage.setItem(`selected_audio_device_${sessionId}`, deviceId);
      if (!isAudioEnabled || !localStream) return;
    }

    try {
      const constraints: MediaStreamConstraints = {};
      if (kind === 'videoinput') {
        constraints.video = { deviceId: { exact: deviceId }, width: { ideal: 1280 }, height: { ideal: 720 } };
        constraints.audio = false;
      } else {
        constraints.video = false;
        constraints.audio = { deviceId: { exact: deviceId }, echoCancellation: true, noiseSuppression: true };
      }

      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      const newTrack = kind === 'videoinput' ? stream.getVideoTracks()[0] : stream.getAudioTracks()[0];
      const oldTrack = kind === 'videoinput' ? localStream.getVideoTracks()[0] : localStream.getAudioTracks()[0];

      if (oldTrack) {
        oldTrack.stop();
        localStream.removeTrack(oldTrack);
      }
      localStream.addTrack(newTrack);

      const sender = peerConnectionRef.current?.getSenders().find(s => s.track?.kind === (kind === 'videoinput' ? 'video' : 'audio'));
      if (sender) {
        await sender.replaceTrack(newTrack);
      }

      // Recreate stream to trigger React rerender for local video element
      setLocalStream(new MediaStream(localStream.getTracks()));

    } catch (error) {
      console.error(`Failed to change ${kind} device`, error);
      onError?.(`Failed to change ${kind === 'videoinput' ? 'camera' : 'microphone'}`);
    }
  };

  // Track remote video availability (for "camera off" placeholder).
  useEffect(() => {
    if (!remoteStream) {
      setHasRemoteVideoTrack(false);
      setIsRemoteVideoMuted(false);
      return;
    }

    const track = remoteStream.getVideoTracks()[0];
    if (!track) {
      setHasRemoteVideoTrack(false);
      setIsRemoteVideoMuted(false);
      return;
    }

    setHasRemoteVideoTrack(true);
    setIsRemoteVideoMuted(track.muted);

    const onMute = () => setIsRemoteVideoMuted(true);
    const onUnmute = () => setIsRemoteVideoMuted(false);
    track.addEventListener('mute', onMute);
    track.addEventListener('unmute', onUnmute);

    return () => {
      track.removeEventListener('mute', onMute);
      track.removeEventListener('unmute', onUnmute);
    };
  }, [remoteStream]);

  useEffect(() => {
    onStateChange?.({
      isVideoEnabled,
      isAudioEnabled,
      hasRemoteStream: Boolean(remoteStream),
    });
  }, [isAudioEnabled, isVideoEnabled, onStateChange, remoteStream]);

  const initializeSignalR = async (): Promise<signalR.HubConnection> => {
    // Get base URL without /api suffix
    const baseUrl = (api.defaults.baseURL && typeof api.defaults.baseURL === 'string') 
      ? api.defaults.baseURL.replace('/api', '') 
      : 'http://localhost:5000';
    
    // Create SignalR connection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${baseUrl}/api/collaboration`, {
        transport: signalR.HttpTransportType.WebSockets,
        accessTokenFactory: () => tokenStorage.getAccessToken() || '',
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

    connection.on('MediaStateUpdated', (data: { userId: string; isVideoEnabled: boolean; isAudioEnabled: boolean }) => {
      if (data.userId !== userId) {
        setIsRemoteVideoMuted(!data.isVideoEnabled);
        // If they explicitly turned off video, we assume they have a track but it's just off
        if (!data.isVideoEnabled) {
          setHasRemoteVideoTrack(true);
        }
      }
    });

    // Start connection
    await connection.start();
    
    // Join the session group
    await connection.invoke('JoinSession', sessionId);

    // Initial broadcast of our local state
    const videoEnabled = getSavedVideoState();
    const audioEnabled = getSavedAudioState();
    await connection.invoke('SendMediaState', sessionId, videoEnabled, audioEnabled).catch(() => {});

    return connection;
  };

  const initializeVideoAndSignaling = async () => {
    try {
      const shouldInitiateOffer = peerUserId
        ? String(userId).localeCompare(String(peerUserId)) < 0
        : true; // fallback if peerUserId isn't known yet

      // Initialize SignalR connection first
      const connection = await initializeSignalR();
      signalRConnectionRef.current = connection;

      // Get saved video/audio states
      const videoEnabled = getSavedVideoState();
      const audioEnabled = getSavedAudioState();

      // Get local media stream with saved preferences
      const stream = await navigator.mediaDevices.getUserMedia({
        video: videoEnabled ? {
          deviceId: selectedVideoDeviceId ? { exact: selectedVideoDeviceId } : undefined,
          width: { ideal: 1280 },
          height: { ideal: 720 },
          facingMode: 'user'
        } : false,
        audio: audioEnabled ? {
          deviceId: selectedAudioDeviceId ? { exact: selectedAudioDeviceId } : undefined,
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
        setConnectionState(peerConnection.connectionState);
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
      // Wait a bit for the other user to join; only one side should initiate.
      setTimeout(async () => {
        if (!shouldInitiateOffer) return;
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

      // If we deterministically chose this client as the offerer but we received an offer anyway,
      // we still accept it to avoid "glare" failures (common in WebRTC).
      isOfferSentRef.current = true;

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

    if (!localStream) {
      setIsVideoEnabled(newState);
      localStorage.setItem(`video_enabled_${sessionId}`, String(newState));
      if (signalRConnectionRef.current?.state === 'Connected') {
        signalRConnectionRef.current.invoke('SendMediaState', sessionId, newState, isAudioEnabled).catch(() => {});
      }
      return;
    }

    const videoTrack = localStream.getVideoTracks()[0];
    if (videoTrack) {
      videoTrack.enabled = newState;
    } else if (newState) {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          video: {
            deviceId: selectedVideoDeviceId ? { exact: selectedVideoDeviceId } : undefined,
            width: { ideal: 1280 },
            height: { ideal: 720 },
            facingMode: 'user',
          },
          audio: false,
        });
        const newVideoTrack = stream.getVideoTracks()[0];
        if (newVideoTrack) {
          // Create a NEW MediaStream to force React to update the local video srcObject properly
          const newStream = new MediaStream([newVideoTrack, ...localStream.getAudioTracks()]);
          setLocalStream(newStream);

          const sender = peerConnectionRef.current?.getSenders().find((s) => s.track?.kind === 'video');
          if (sender) {
            await sender.replaceTrack(newVideoTrack);
          } else {
            peerConnectionRef.current?.addTrack(newVideoTrack, newStream);
          }
        }
      } catch (error) {
        console.error('Failed to enable video:', error);
        onError?.('Failed to enable camera');
        return;
      }
    }
    
    setIsVideoEnabled(newState);
    localStorage.setItem(`video_enabled_${sessionId}`, String(newState));
    if (signalRConnectionRef.current?.state === 'Connected') {
      signalRConnectionRef.current.invoke('SendMediaState', sessionId, newState, isAudioEnabled).catch(() => {});
    }
  };

  const toggleAudio = async () => {
    const newState = !isAudioEnabled;

    if (!localStream) {
      setIsAudioEnabled(newState);
      localStorage.setItem(`audio_enabled_${sessionId}`, String(newState));
      if (signalRConnectionRef.current?.state === 'Connected') {
        signalRConnectionRef.current.invoke('SendMediaState', sessionId, isVideoEnabled, newState).catch(() => {});
      }
      return;
    }

    const audioTrack = localStream.getAudioTracks()[0];
    if (audioTrack) {
      audioTrack.enabled = newState;
    } else if (newState) {
      try {
        const stream = await navigator.mediaDevices.getUserMedia({
          video: false,
          audio: {
            deviceId: selectedAudioDeviceId ? { exact: selectedAudioDeviceId } : undefined,
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true,
          },
        });
        const newAudioTrack = stream.getAudioTracks()[0];
        if (newAudioTrack) {
          // Create a NEW MediaStream to trigger effect hooks naturally
          const newStream = new MediaStream([...localStream.getVideoTracks(), newAudioTrack]);
          setLocalStream(newStream);

          const sender = peerConnectionRef.current?.getSenders().find((s) => s.track?.kind === 'audio');
          if (sender) {
            await sender.replaceTrack(newAudioTrack);
          } else {
            peerConnectionRef.current?.addTrack(newAudioTrack, newStream);
          }
        }
      } catch (error) {
        console.error('Failed to enable audio:', error);
        onError?.('Failed to enable microphone');
        return;
      }
    }
    
    setIsAudioEnabled(newState);
    localStorage.setItem(`audio_enabled_${sessionId}`, String(newState));
    if (signalRConnectionRef.current?.state === 'Connected') {
      signalRConnectionRef.current.invoke('SendMediaState', sessionId, isVideoEnabled, newState).catch(() => {});
    }
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

  useImperativeHandle(ref, () => ({
    toggleVideo,
    toggleAudio,
    getState: () => ({
      isVideoEnabled,
      isAudioEnabled,
      hasRemoteStream: Boolean(remoteStream),
    }),
  }), [isAudioEnabled, isVideoEnabled, remoteStream]);

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
            {connectionState !== 'connected' && connectionState !== 'connecting' && (
              <div className="connection-status-overlay">
                <i className="fas fa-signal"></i>
                <span>{connectionState}</span>
              </div>
            )}
            {!remoteStream && (
              <div className="video-overlay">
                <i className="fas fa-user-slash"></i>
                <span>Waiting for partner...</span>
              </div>
            )}
            {remoteStream && (!hasRemoteVideoTrack || isRemoteVideoMuted) && (
              <div className="video-overlay">
                <i className="fas fa-video-slash"></i>
                <span>Partner camera is off</span>
              </div>
            )}
            {showLocalPreview && localStream && (
              <div
                ref={localPreviewRef}
                className="local-video-preview"
                aria-label="Your camera preview"
                role="group"
              >
                <video
                  ref={localVideoRef}
                  autoPlay
                  muted
                  playsInline
                  className="preview-video-element"
                />
                {!isVideoEnabled ? (
                  <div className="video-overlay">
                    <i className="fas fa-video-slash"></i>
                  </div>
                ) : null}
              </div>
            )}
            {!hideControls && overlayControls && (
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
      {!hideControls && !overlayControls && (
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
          <button
            className="control-btn"
            onClick={() => setShowSettings(true)}
            title="Settings"
          >
            <i className="fas fa-cog"></i>
          </button>
        </div>
      )}
      
      {showSettings && (
        <div className="device-settings-modal">
          <div className="device-settings-content">
            <h3>Device Settings</h3>
            <div className="form-group">
              <label>Camera</label>
              <select 
                value={selectedVideoDeviceId} 
                onChange={(e) => changeDevice('videoinput', e.target.value)}
              >
                {devices.filter(d => d.kind === 'videoinput').map(d => (
                  <option key={d.deviceId} value={d.deviceId}>{d.label || `Camera ${d.deviceId.substring(0, 5)}`}</option>
                ))}
              </select>
            </div>
            <div className="form-group">
              <label>Microphone</label>
              <select 
                value={selectedAudioDeviceId} 
                onChange={(e) => changeDevice('audioinput', e.target.value)}
              >
                {devices.filter(d => d.kind === 'audioinput').map(d => (
                  <option key={d.deviceId} value={d.deviceId}>{d.label || `Microphone ${d.deviceId.substring(0, 5)}`}</option>
                ))}
              </select>
            </div>
            <div className="settings-actions">
              <button onClick={() => setShowSettings(false)}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
});

VideoChat.displayName = 'VideoChat';
