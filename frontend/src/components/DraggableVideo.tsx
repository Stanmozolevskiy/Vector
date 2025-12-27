import React, { useEffect, useRef, useState } from 'react';
import './DraggableVideo.css';

interface DraggableVideoProps {
  sessionId: string;
  userId: string;
  peerUserId?: string;
  onError?: (error: string) => void;
  onClose?: () => void;
}

export const DraggableVideo: React.FC<DraggableVideoProps> = ({
  sessionId,
  // userId, // Reserved for future use
  // peerUserId, // Reserved for future use
  onError,
  onClose,
}) => {
  const remoteVideoRef = useRef<HTMLVideoElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [position, setPosition] = useState({ x: window.innerWidth - 320, y: 20 });
  const [isMinimized, setIsMinimized] = useState(false);
  const [isVideoEnabled, setIsVideoEnabled] = useState(true);
  const [isAudioEnabled, setIsAudioEnabled] = useState(true);
  const [remoteStream, setRemoteStream] = useState<MediaStream | null>(null);
  const peerConnectionRef = useRef<RTCPeerConnection | null>(null);
  const dragStartPos = useRef({ x: 0, y: 0 });

  useEffect(() => {
    initializeVideo();
    return () => {
      cleanup();
    };
  }, [sessionId]);

  const initializeVideo = async () => {
    try {
      // Initialize WebRTC peer connection
      const configuration = {
        iceServers: [
          { urls: 'stun:stun.l.google.com:19302' },
        ],
      };

      const peerConnection = new RTCPeerConnection(configuration);
      peerConnectionRef.current = peerConnection;

      // Handle remote stream
      peerConnection.ontrack = (event) => {
        if (remoteVideoRef.current) {
          remoteVideoRef.current.srcObject = event.streams[0];
          setRemoteStream(event.streams[0]);
        }
      };

      // Handle ICE candidates
      peerConnection.onicecandidate = (event) => {
        if (event.candidate) {
          // Send ICE candidate to peer via signaling server
          // This would be implemented with your backend signaling
        }
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to initialize video';
      onError?.(errorMessage);
    }
  };

  const cleanup = () => {
    if (remoteStream) {
      remoteStream.getTracks().forEach((track) => track.stop());
    }
    if (peerConnectionRef.current) {
      peerConnectionRef.current.close();
    }
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    if ((e.target as HTMLElement).closest('.video-controls, .minimize-btn, .close-btn')) {
      return;
    }
    setIsDragging(true);
    dragStartPos.current = {
      x: e.clientX - position.x,
      y: e.clientY - position.y,
    };
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (!isDragging) return;
    
    const newX = e.clientX - dragStartPos.current.x;
    const newY = e.clientY - dragStartPos.current.y;
    
    // Constrain to viewport
    const maxX = window.innerWidth - (containerRef.current?.offsetWidth || 300);
    const maxY = window.innerHeight - (containerRef.current?.offsetHeight || 200);
    
    setPosition({
      x: Math.max(0, Math.min(newX, maxX)),
      y: Math.max(0, Math.min(newY, maxY)),
    });
  };

  const handleMouseUp = () => {
    setIsDragging(false);
  };

  useEffect(() => {
    if (isDragging) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging]);

  const toggleMinimize = () => {
    setIsMinimized(!isMinimized);
  };

  const toggleVideo = () => {
    setIsVideoEnabled(!isVideoEnabled);
    // Video toggle would affect local stream, but this is remote video
    // This is for UI state only
  };

  const toggleAudio = () => {
    setIsAudioEnabled(!isAudioEnabled);
    // Audio toggle would affect local stream, but this is remote video
    // This is for UI state only
  };

  return (
    <div
      ref={containerRef}
      className={`draggable-video-container ${isMinimized ? 'minimized' : ''}`}
      style={{
        left: `${position.x}px`,
        top: `${position.y}px`,
      }}
      onMouseDown={handleMouseDown}
    >
      <div className="video-header">
        <span className="video-title">Partner Video</span>
        <div className="video-header-controls">
          <button
            className="header-btn"
            onClick={toggleMinimize}
            title={isMinimized ? 'Restore' : 'Minimize'}
          >
            <i className={`fas ${isMinimized ? 'fa-window-restore' : 'fa-window-minimize'}`}></i>
          </button>
          {onClose && (
            <button
              className="header-btn close-btn"
              onClick={onClose}
              title="Close"
            >
              <i className="fas fa-times"></i>
            </button>
          )}
        </div>
      </div>
      {!isMinimized && (
        <>
          <div className="video-content">
            <video
              ref={remoteVideoRef}
              autoPlay
              playsInline
              className="video-element"
            />
            {!remoteStream && (
              <div className="video-placeholder">
                <i className="fas fa-user"></i>
                <p>Waiting for partner...</p>
              </div>
            )}
          </div>
          <div className="video-controls">
            <button
              className={`control-btn ${isVideoEnabled ? 'active' : ''}`}
              onClick={toggleVideo}
              title={isVideoEnabled ? 'Video enabled' : 'Video disabled'}
            >
              <i className={`fas ${isVideoEnabled ? 'fa-video' : 'fa-video-slash'}`}></i>
            </button>
            <button
              className={`control-btn ${isAudioEnabled ? 'active' : ''}`}
              onClick={toggleAudio}
              title={isAudioEnabled ? 'Audio enabled' : 'Audio disabled'}
            >
              <i className={`fas ${isAudioEnabled ? 'fa-volume-up' : 'fa-volume-mute'}`}></i>
            </button>
          </div>
        </>
      )}
    </div>
  );
};

