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
  const peerConnectionRef = useRef<RTCPeerConnection | null>(null);
  const signalRConnectionRef = useRef<signalR.HubConnection | null>(null);
  const isOfferSentRef = useRef<boolean>(false);
  const screenStreamRef = useRef<MediaStream | null>(null);

  type PreviewRect = { x: number; y: number; width: number; height: number };

  const getDefaultPreviewRect = (): PreviewRect => {
    const width = 140;
    const height = 105;
    const right = 18;
    const top = 18;
    const vw = typeof window !== 'undefined' ? window.innerWidth : 1280;
    const x = Math.max(8, vw - right - width);
    const y = top;
    return { x, y, width, height };
  };

  const previewStorageKey = `local_preview_rect_${sessionId}`;
  const [previewRect, setPreviewRect] = useState<PreviewRect>(() => {
    try {
      const raw = localStorage.getItem(previewStorageKey);
      if (!raw) return getDefaultPreviewRect();
      const parsed = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object') return getDefaultPreviewRect();
      const x = Number(parsed.x);
      const y = Number(parsed.y);
      const width = Number(parsed.width);
      const height = Number(parsed.height);
      if (![x, y, width, height].every(Number.isFinite)) return getDefaultPreviewRect();
      return { x, y, width, height };
    } catch {
      return getDefaultPreviewRect();
    }
  });

  const interactionRef = useRef<
    | null
    | { mode: 'move'; startX: number; startY: number; originX: number; originY: number }
    | { mode: 'resize'; startX: number; startY: number; originW: number; originH: number }
  >(null);

  useEffect(() => {
    try {
      localStorage.setItem(previewStorageKey, JSON.stringify(previewRect));
    } catch {
      // ignore
    }
  }, [previewRect, previewStorageKey]);

  useEffect(() => {
    const onWindowResize = () => {
      const vw = window.innerWidth;
      const vh = window.innerHeight;
      setPreviewRect((r) => {
        const minW = 120;
        const minH = 90;
        const width = Math.max(minW, Math.min(r.width, vw - 8));
        const height = Math.max(minH, Math.min(r.height, vh - 8));
        const x = Math.max(8, Math.min(r.x, vw - width - 8));
        const y = Math.max(8, Math.min(r.y, vh - height - 8));
        return { x, y, width, height };
      });
    };
    window.addEventListener('resize', onWindowResize);
    return () => window.removeEventListener('resize', onWindowResize);
  }, []);

  const clampPreviewRect = (next: PreviewRect): PreviewRect => {
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const minW = 120;
    const minH = 90;
    const width = Math.max(minW, Math.min(next.width, vw - 8));
    const height = Math.max(minH, Math.min(next.height, vh - 8));
    const x = Math.max(8, Math.min(next.x, vw - width - 8));
    const y = Math.max(8, Math.min(next.y, vh - height - 8));
    return { x, y, width, height };
  };

  const handlePreviewPointerDown = (e: React.PointerEvent) => {
    if (!showLocalPreview) return;
    if (!localStream) return;
    if ((e.target as HTMLElement | null)?.closest?.('.local-preview-resize-handle')) return;

    (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
    interactionRef.current = {
      mode: 'move',
      startX: e.clientX,
      startY: e.clientY,
      originX: previewRect.x,
      originY: previewRect.y,
    };
  };

  const handleResizePointerDown = (e: React.PointerEvent) => {
    if (!showLocalPreview) return;
    if (!localStream) return;

    e.stopPropagation();
    (localPreviewRef.current as HTMLElement | null)?.setPointerCapture?.(e.pointerId);
    interactionRef.current = {
      mode: 'resize',
      startX: e.clientX,
      startY: e.clientY,
      originW: previewRect.width,
      originH: previewRect.height,
    };
  };

  const handlePreviewPointerMove = (e: React.PointerEvent) => {
    const state = interactionRef.current;
    if (!state) return;

    if (state.mode === 'move') {
      const dx = e.clientX - state.startX;
      const dy = e.clientY - state.startY;
      setPreviewRect((r) => clampPreviewRect({ ...r, x: state.originX + dx, y: state.originY + dy }));
      return;
    }

    const dx = e.clientX - state.startX;
    const dy = e.clientY - state.startY;
    setPreviewRect((r) => clampPreviewRect({ ...r, width: state.originW + dx, height: state.originH + dy }));
  };

  const handlePreviewPointerUp = () => {
    interactionRef.current = null;
  };

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
                style={{
                  left: `${previewRect.x}px`,
                  top: `${previewRect.y}px`,
                  width: `${previewRect.width}px`,
                  height: `${previewRect.height}px`,
                  right: 'auto',
                  bottom: 'auto',
                }}
                onPointerDown={handlePreviewPointerDown}
                onPointerMove={handlePreviewPointerMove}
                onPointerUp={handlePreviewPointerUp}
                onPointerCancel={handlePreviewPointerUp}
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
                <div
                  className="local-preview-resize-handle"
                  role="button"
                  aria-label="Resize preview"
                  tabIndex={0}
                  onPointerDown={handleResizePointerDown}
                  onKeyDown={(ev) => {
                    if (ev.key === 'Enter' || ev.key === ' ') {
                      ev.preventDefault();
                    }
                  }}
                />
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
        </div>
      )}
    </div>
  );
});

VideoChat.displayName = 'VideoChat';
