import React, { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { CodeEditor } from './CodeEditor';
import './CollaborativeCodeEditor.css';
import api from '../services/api';


interface CollaborativeCodeEditorProps {
  value: string;
  language: string;
  onChange: (value: string) => void;
  sessionId: string;
  userId: string;
  peerUserId?: string;
  isInterviewer?: boolean;
  onError?: (error: string) => void;
}

export const CollaborativeCodeEditor: React.FC<CollaborativeCodeEditorProps> = ({
  value,
  language,
  onChange,
  sessionId,
  userId,
  peerUserId: _peerUserId, // Reserved for future use
  isInterviewer = false,
  onError,
}) => {
  const [remoteCursors, setRemoteCursors] = useState<Array<{ userId: string; line: number; column: number; color: string }>>([]);
  const [remoteSelections, setRemoteSelections] = useState<Array<{ userId: string; startLine: number; startColumn: number; endLine: number; endColumn: number; color: string }>>([]);
  const editorRef = useRef<any>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const debounceTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const selectionDebounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    initializeSignalR();
    return () => {
      cleanup();
    };
  }, [sessionId]);

  const initializeSignalR = async () => {
    try {
      // Get access token for authentication
      const accessToken = localStorage.getItem('accessToken');
      if (!accessToken) {
        onError?.('Authentication required');
        return;
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
        .configureLogging(signalR.LogLevel.Warning) // Only show warnings and errors, not Information
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

      // Set up event handlers
      connection.on('CodeChanged', (data: { userId: string; code: string; timestamp: string }) => {
        if (data.userId !== userId) {
          onChange(data.code);
        }
      });

      connection.on('CursorMoved', (data: { userId: string; line: number; column: number; color: string }) => {
        if (data.userId !== userId) {
          setRemoteCursors((prev) => {
            const filtered = prev.filter((c) => c.userId !== data.userId);
            return [...filtered, { userId: data.userId, line: data.line, column: data.column, color: data.color }];
          });
        }
      });

      connection.on('SelectionChanged', (data: { userId: string; startLine: number; startColumn: number; endLine: number; endColumn: number; color: string } | null) => {
        if (data && data.userId !== userId) {
          setRemoteSelections((prev) => {
            const filtered = prev.filter((s) => s.userId !== data.userId);
            if (data.startLine && data.endLine) {
              return [...filtered, { 
                userId: data.userId, 
                startLine: data.startLine,
                startColumn: data.startColumn,
                endLine: data.endLine,
                endColumn: data.endColumn,
                color: data.color
              }];
            }
            return filtered;
          });
        } else if (!data) {
          // Clear selection for all users (when selection is cleared)
          setRemoteSelections([]);
        }
      });

      connection.on('UserJoined', (_data: { userId: string; connectionId: string }) => {
        // User joined - can be used for future features
      });

      connection.on('UserLeft', (data: { userId: string; connectionId: string }) => {
        // Remove cursors and selections when user leaves
        setRemoteCursors((prev) => prev.filter((c) => c.userId !== data.userId));
        setRemoteSelections((prev) => prev.filter((s) => s.userId !== data.userId));
      });

      connection.on('Error', (error: string) => {
        onError?.(error);
      });

      connection.onclose((error) => {
        if (error) {
          onError?.('Connection closed with error');
        }
      });

      // Start connection
      await connection.start();
      connectionRef.current = connection;

      // Join the session
      await connection.invoke('JoinSession', sessionId);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to initialize collaboration';
      onError?.(errorMessage);
    }
  };



  const handleCodeChange = (newValue: string | undefined) => {
    const code = newValue || '';
    onChange(code);

    // Debounce SignalR messages
    if (debounceTimeoutRef.current) {
      clearTimeout(debounceTimeoutRef.current);
    }

    debounceTimeoutRef.current = setTimeout(async () => {
      if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
        try {
          await connectionRef.current.invoke('SendCodeChange', sessionId, code);
        } catch (error) {
          // Silently fail - connection might be reconnecting
        }
      }
    }, 300);
  };

  const handleCursorChange = (line: number, column: number) => {
    if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
      connectionRef.current.invoke('SendCursorPosition', sessionId, line, column).catch(() => {
        // Silently fail
      });
    }
  };

  const handleSelectionChange = (selection: { startLine: number; startColumn: number; endLine: number; endColumn: number } | null) => {
    if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
      // Debounce selection changes
      if (selectionDebounceRef.current) {
        clearTimeout(selectionDebounceRef.current);
      }
      
      // Use different colors for interviewer vs interviewee
      const color = isInterviewer ? '#7c3aed' : '#10b981'; // Purple for interviewer, green for interviewee
      
      selectionDebounceRef.current = setTimeout(() => {
        if (connectionRef.current && connectionRef.current.state === signalR.HubConnectionState.Connected) {
          connectionRef.current.invoke('SendSelection', sessionId, selection, color).catch(() => {
            // Silently fail
          });
        }
      }, 100);
    }
  };

  // Cursor change handler - can be used when CodeEditor supports onCursorChange callback
  // const handleCursorChange = (line: number, column: number) => {
  //   if (wsRef.current && wsRef.current.readyState === WebSocket.OPEN) {
  //     const userColor = getUserColor(userId);
  //     wsRef.current.send(JSON.stringify({
  //       type: 'cursor-move',
  //       userId,
  //       sessionId,
  //       line,
  //       column,
  //       color: userColor,
  //     }));
  //   }
  // };

  // Generate a consistent color for each user (reserved for future cursor tracking)
  // const getUserColor = (userIdParam: string): string => {
  //   const colors = ['#7c3aed', '#10b981', '#f59e0b', '#ef4444', '#3b82f6'];
  //   const index = userIdParam.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0) % colors.length;
  //   return colors[index];
  // };

  const cleanup = async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop();
      } catch (error) {
        // Ignore errors during cleanup
      }
      connectionRef.current = null;
    }
    if (debounceTimeoutRef.current) {
      clearTimeout(debounceTimeoutRef.current);
    }
    if (selectionDebounceRef.current) {
      clearTimeout(selectionDebounceRef.current);
    }
  };

  return (
    <div className="collaborative-editor-container">
      <div className="editor-wrapper">
        <CodeEditor
          value={value}
          language={language}
          onChange={handleCodeChange}
          onCursorChange={handleCursorChange}
          onSelectionChange={handleSelectionChange}
          remoteCursors={remoteCursors}
          remoteSelections={remoteSelections}
          height="100%"
          theme="light"
          readOnly={false}
          editorRef={editorRef}
        />
      </div>
      {/* Cursors are now rendered via Monaco decorations in CodeEditor */}
    </div>
  );
};

