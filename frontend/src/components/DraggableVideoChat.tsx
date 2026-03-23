import React, { useEffect, useRef, useState } from 'react';
import { VideoChat } from './VideoChat';
import './DraggableVideo.css';

interface DraggableVideoChatProps {
  sessionId: string;
  userId: string;
  peerUserId?: string;
  onError?: (error: string) => void;
}

export const DraggableVideoChat: React.FC<DraggableVideoChatProps> = ({
  sessionId,
  userId,
  peerUserId,
  onError,
}) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [isResizing, setIsResizing] = useState(false);
  const [position, setPosition] = useState({ 
    x: Math.max(20, window.innerWidth - 320), 
    y: 20 
  });
  const [size, setSize] = useState({ width: 300, height: 225 }); // 16:9 aspect ratio for 300px width
  const dragStartPos = useRef({ x: 0, y: 0 });
  const resizeStartPos = useRef({ x: 0, y: 0, width: 0, height: 0 });

  // Reset position on window resize
  useEffect(() => {
    const handleResize = () => {
      setPosition(prev => ({
        x: Math.min(prev.x, window.innerWidth - size.width),
        y: Math.min(prev.y, window.innerHeight - size.height)
      }));
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [size]);

  const handleHeaderMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    setIsDragging(true);
    dragStartPos.current = {
      x: e.clientX - position.x,
      y: e.clientY - position.y,
    };
  };

  const handleResizeMouseDown = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsResizing(true);
    resizeStartPos.current = {
      x: e.clientX,
      y: e.clientY,
      width: size.width,
      height: size.height,
    };
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (isResizing) {
      const deltaX = e.clientX - resizeStartPos.current.x;
      
      // Calculate new size maintaining aspect ratio (16:9)
      const aspectRatio = 16 / 9;
      const newWidth = Math.max(250, Math.min(600, resizeStartPos.current.width + deltaX));
      const newHeight = newWidth / aspectRatio;
      
      setSize({
        width: newWidth,
        height: newHeight,
      });
      
      // Adjust position if resizing from top or left would push outside viewport
      const maxX = window.innerWidth - newWidth;
      const maxY = window.innerHeight - newHeight;
      
      if (position.x > maxX) {
        setPosition(prev => ({ ...prev, x: Math.max(0, maxX) }));
      }
      if (position.y > maxY) {
        setPosition(prev => ({ ...prev, y: Math.max(0, maxY) }));
      }
    } else if (isDragging) {
      const newX = e.clientX - dragStartPos.current.x;
      const newY = e.clientY - dragStartPos.current.y;
      
      // Constrain to viewport
      const maxX = window.innerWidth - size.width;
      const maxY = window.innerHeight - size.height;
      
      setPosition({
        x: Math.max(0, Math.min(newX, maxX)),
        y: Math.max(0, Math.min(newY, maxY)),
      });
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    setIsResizing(false);
  };

  useEffect(() => {
    if (isDragging || isResizing) {
      document.addEventListener('mousemove', handleMouseMove);
      document.addEventListener('mouseup', handleMouseUp);
      return () => {
        document.removeEventListener('mousemove', handleMouseMove);
        document.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, isResizing]);

  return (
    <div
      ref={containerRef}
      className="draggable-video-container"
      style={{
        left: `${position.x}px`,
        top: `${position.y}px`,
        width: `${size.width}px`,
        height: `${size.height}px`,
        cursor: isDragging ? 'grabbing' : (isResizing ? 'se-resize' : 'default'),
      }}
    >
      <div 
        className="video-drag-handle"
        onMouseDown={handleHeaderMouseDown}
      >
        <span className="video-title">Session Video</span>
      </div>
      <div className="draggable-video-content">
        <VideoChat
          sessionId={sessionId}
          userId={userId}
          peerUserId={peerUserId}
          onError={onError}
          showLocalVideo={false}
          showLocalPreview={true}
          overlayControls={true}
        />
      </div>
      <div 
        className="resize-handle"
        onMouseDown={handleResizeMouseDown}
      />
    </div>
  );
};
