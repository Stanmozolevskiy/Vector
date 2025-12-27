import React, { useEffect, useRef, useState } from 'react';
import './VideoChat.css';
import { videoSessionService } from '../services/videoSession.service';

interface VideoChatProps {
  sessionId: string;
  userId: string;
  peerUserId?: string;
  onError?: (error: string) => void;
}

export const VideoChat: React.FC<VideoChatProps> = ({
  sessionId,
  // userId, // Reserved for future use (e.g., user identification in video)
  // peerUserId, // Reserved for future use (e.g., peer identification)
  onError,
}) => {
  const localVideoRef = useRef<HTMLVideoElement>(null);
  const remoteVideoRef = useRef<HTMLVideoElement>(null);
  const [isVideoEnabled, setIsVideoEnabled] = useState(true);
  const [isAudioEnabled, setIsAudioEnabled] = useState(true);
  const [isScreenSharing, setIsScreenSharing] = useState(false);
  const [localStream, setLocalStream] = useState<MediaStream | null>(null);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
  const peerConnectionRef = useRef<RTCPeerConnection | null>(null);
  const videoSessionIdRef = useRef<string | null>(null);
  const signalingPollIntervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const isInitiatorRef = useRef<boolean>(false);

  useEffect(() => {
    initializeVideo();
    return () => {
      cleanup();
    };
  }, [sessionId]);

  const initializeVideo = async () => {
    try {
      // Create or get video session
      let videoSessionId: string;
      try {
        const tokenResponse = await videoSessionService.getVideoSessionToken(sessionId);
        videoSessionId = tokenResponse.videoSessionId;
      } catch {
        const videoSession = await videoSessionService.createVideoSession(sessionId);
        videoSessionId = videoSession.id;
      }
      videoSessionIdRef.current = videoSessionId;

      // Get local media stream
      const stream = await navigator.mediaDevices.getUserMedia({
        video: true,
        audio: true,
      });

      if (localVideoRef.current) {
        localVideoRef.current.srcObject = stream;
      }

      setLocalStream(stream);

      // Initialize WebRTC peer connection
      const configuration = {
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
          { urls: 'stun:stun1.l.google.com:19302' },
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
        if (event.candidate && videoSessionIdRef.current) {
          try {
            await videoSessionService.sendIceCandidate(
              videoSessionIdRef.current,
              JSON.stringify(event.candidate),
              event.candidate.sdpMLineIndex ?? undefined,
              event.candidate.sdpMid ?? undefined
            );
          } catch (error) {
            // Silently fail - signaling might not be ready yet
          }
        }
      };

      // Start signaling process
      await startSignaling(videoSessionId);

      // Poll for signaling data (offers, answers, ICE candidates)
      startSignalingPoll(videoSessionId);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to access camera/microphone';
      onError?.(errorMessage);
    }
  };

  const startSignaling = async (videoSessionId: string) => {
    try {
      // Check if there's already an offer (another user started)
      const signalingData = await videoSessionService.getSignalingData(videoSessionId);
      
      if (signalingData.signalingData) {
        try {
          const data = JSON.parse(signalingData.signalingData);
          if (data.offer) {
            // Another user already sent an offer, create answer
            await handleOffer(videoSessionId, data.offer);
            isInitiatorRef.current = false;
            return;
          } else if (data.answer) {
            // Answer received, set it
            await handleAnswer(data.answer);
            isInitiatorRef.current = true;
            return;
          }
        } catch {
          // Invalid JSON, continue to create offer
        }
      }

      // Create offer (we're the initiator)
      if (peerConnectionRef.current) {
        const offer = await peerConnectionRef.current.createOffer();
        await peerConnectionRef.current.setLocalDescription(offer);
        await videoSessionService.sendOffer(videoSessionId, JSON.stringify(offer));
        isInitiatorRef.current = true;
      }
    } catch (error) {
      onError?.('Failed to start signaling');
    }
  };

  const handleOffer = async (videoSessionId: string, offerString: string) => {
    try {
      if (!peerConnectionRef.current) return;

      const offer = JSON.parse(offerString);
      await peerConnectionRef.current.setRemoteDescription(new RTCSessionDescription(offer));

      // Create and send answer
      const answer = await peerConnectionRef.current.createAnswer();
      await peerConnectionRef.current.setLocalDescription(answer);
      await videoSessionService.sendAnswer(videoSessionId, JSON.stringify(answer));
    } catch (error) {
      onError?.('Failed to handle offer');
    }
  };

  const handleAnswer = async (answerString: string) => {
    try {
      if (!peerConnectionRef.current) return;

      const answer = JSON.parse(answerString);
      await peerConnectionRef.current.setRemoteDescription(new RTCSessionDescription(answer));
    } catch (error) {
      onError?.('Failed to handle answer');
    }
  };

  const handleIceCandidate = async (candidateString: string) => {
    try {
      if (!peerConnectionRef.current) return;

      const candidate = JSON.parse(candidateString);
      await peerConnectionRef.current.addIceCandidate(new RTCIceCandidate(candidate));
    } catch (error) {
      // Silently fail - candidate might already be added
    }
  };

  const startSignalingPoll = (videoSessionId: string) => {
    // Poll for signaling data every 2 seconds
    signalingPollIntervalRef.current = setInterval(async () => {
      try {
        const signalingData = await videoSessionService.getSignalingData(videoSessionId);
        if (signalingData.signalingData) {
          try {
            const data = JSON.parse(signalingData.signalingData);
            
            if (data.offer && !isInitiatorRef.current) {
              await handleOffer(videoSessionId, data.offer);
            } else if (data.answer && isInitiatorRef.current) {
              await handleAnswer(data.answer);
            } else if (data.iceCandidate) {
              await handleIceCandidate(data.iceCandidate.candidate);
            }
          } catch {
            // Invalid JSON, ignore
          }
        }
      } catch (error) {
        // Silently fail - connection might be establishing
      }
    }, 2000);
  };

  const cleanup = async () => {
    if (signalingPollIntervalRef.current) {
      clearInterval(signalingPollIntervalRef.current);
    }
    if (localStream) {
      localStream.getTracks().forEach((track) => track.stop());
    }
    if (remoteStream) {
      remoteStream.getTracks().forEach((track) => track.stop());
    }
    if (peerConnectionRef.current) {
      peerConnectionRef.current.close();
    }
    if (videoSessionIdRef.current) {
      try {
        await videoSessionService.endVideoSession(videoSessionIdRef.current);
      } catch {
        // Silently fail during cleanup
      }
    }
  };

  const toggleVideo = () => {
    if (localStream) {
      const videoTrack = localStream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.enabled = !isVideoEnabled;
        setIsVideoEnabled(!isVideoEnabled);
      }
    }
  };

  const toggleAudio = () => {
    if (localStream) {
      const audioTrack = localStream.getAudioTracks()[0];
      if (audioTrack) {
        audioTrack.enabled = !isAudioEnabled;
        setIsAudioEnabled(!isAudioEnabled);
      }
    }
  };

  const toggleScreenShare = async () => {
    try {
      if (!isScreenSharing) {
        const screenStream = await navigator.mediaDevices.getDisplayMedia({
          video: true,
          audio: true,
        });

        if (localVideoRef.current) {
          localVideoRef.current.srcObject = screenStream;
        }

        // Replace video track in peer connection
        const videoTrack = screenStream.getVideoTracks()[0];
        if (peerConnectionRef.current && localStream) {
          const sender = peerConnectionRef.current
            .getSenders()
            .find((s) => s.track && s.track.kind === 'video');
          if (sender && videoTrack) {
            await sender.replaceTrack(videoTrack);
          }
        }

        screenStream.getVideoTracks()[0].onended = () => {
          toggleScreenShare();
        };

        setIsScreenSharing(true);
      } else {
        // Switch back to camera
        const cameraStream = await navigator.mediaDevices.getUserMedia({
          video: true,
          audio: true,
        });

        if (localVideoRef.current) {
          localVideoRef.current.srcObject = cameraStream;
        }

        const videoTrack = cameraStream.getVideoTracks()[0];
        if (peerConnectionRef.current) {
          const sender = peerConnectionRef.current
            .getSenders()
            .find((s) => s.track && s.track.kind === 'video');
          if (sender && videoTrack) {
            await sender.replaceTrack(videoTrack);
          }
        }

        setIsScreenSharing(false);
      }
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to share screen';
      onError?.(errorMessage);
    }
  };

  return (
    <div className="video-chat-container">
      <div className="video-chat-grid">
        <div className="video-wrapper">
          <video
            ref={localVideoRef}
            autoPlay
            muted
            playsInline
            className="video-element local-video"
          />
          <div className="video-label">You</div>
        </div>
        <div className="video-wrapper">
          <video
            ref={remoteVideoRef}
            autoPlay
            playsInline
            className="video-element remote-video"
          />
          <div className="video-label">Peer</div>
        </div>
      </div>
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
    </div>
  );
};

