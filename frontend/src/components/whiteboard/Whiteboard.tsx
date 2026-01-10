import { useState, useEffect, useCallback, useRef } from 'react';
import { Excalidraw } from '@alkemio/excalidraw';

// Define local types based on Excalidraw's structure
interface ExcalidrawInitialDataState {
  elements?: readonly any[];
  appState?: any;
  files?: Record<string, any>;
}

interface ExcalidrawImperativeAPI {
  getSceneElements: () => readonly any[];
  updateScene: (data: { elements?: readonly any[]; appState?: any }) => void;
  getAppState: () => any;
  getFiles: () => Record<string, any>;
}

interface WhiteboardProps {
  initialData?: ExcalidrawInitialDataState;
  onSave?: (data: ExcalidrawInitialDataState) => void | Promise<void>;
  onExport?: (data: ExcalidrawInitialDataState) => void;
  readOnly?: boolean;
  gridEnabled?: boolean;
}

export const Whiteboard = ({
  initialData,
  onSave,
  onExport,
  readOnly = false,
  gridEnabled = false,
}: WhiteboardProps) => {
  const [excalidrawAPI, setExcalidrawAPI] = useState<ExcalidrawImperativeAPI | null>(null);
  const [appState, setAppState] = useState<any>({});
  const [files, setFiles] = useState<Record<string, any>>({});
  const [hasLoadedInitialData, setHasLoadedInitialData] = useState(false);
  const [isReady, setIsReady] = useState(false);
  const saveTimeoutRef = useRef<number | null>(null);

  // Load initial data using API instead of initialData prop to avoid state structure issues
  useEffect(() => {
    if (excalidrawAPI && initialData && !hasLoadedInitialData) {
      // Wait for Excalidraw to fully initialize before loading data
      const timer = setTimeout(() => {
        try {
          // Only load elements, let Excalidraw handle appState internally
          if (initialData.elements && Array.isArray(initialData.elements) && initialData.elements.length > 0) {
            excalidrawAPI.updateScene({
              elements: initialData.elements,
            });
          }
        } catch (error) {
          console.error('Failed to load initial data:', error);
        } finally {
          // Mark as loaded after a delay to ensure Excalidraw is stable
          setTimeout(() => {
            setHasLoadedInitialData(true);
            setIsReady(true);
          }, 300);
        }
      }, 200);
      
      return () => clearTimeout(timer);
    } else if (excalidrawAPI && !initialData && !isReady) {
      // No initial data, mark as ready after short delay to ensure Excalidraw initialized
      const timer = setTimeout(() => {
        setHasLoadedInitialData(true);
        setIsReady(true);
      }, 300);
      
      return () => clearTimeout(timer);
    }
  }, [excalidrawAPI, initialData, hasLoadedInitialData, isReady]);

  // Handle changes to the whiteboard - debounce saves to prevent infinite loops
  // Always provide a function (not conditionally undefined) to avoid re-renders
  const handleChange = useCallback(
    (updatedElements: readonly any[], updatedAppState: any, updatedFiles: Record<string, any>) => {
      // Always update local state
      setAppState(updatedAppState);
      setFiles(updatedFiles);

      // Only save if component is ready and this isn't during initial load
      // Use debouncing to prevent rapid successive saves and infinite loops
      if (onSave && isReady && hasLoadedInitialData) {
        // Clear existing timeout
        if (saveTimeoutRef.current !== null) {
          window.clearTimeout(saveTimeoutRef.current);
        }
        
        // Debounce save by 500ms to prevent infinite loops
        saveTimeoutRef.current = window.setTimeout(async () => {
          try {
            // Sanitize appState before saving - remove non-serializable fields
            const sanitizedAppState = { ...updatedAppState };
            // Remove fields that don't serialize or cause issues
            const fieldsToRemove = [
              'collaborators',
              'snapLines',
              'searchMatches',
              'editingLinearElement',
              'selectedLinearElement',
              'multiElement',
              'selectionElement',
            ];
            
            fieldsToRemove.forEach((field) => {
              if (field in sanitizedAppState) {
                delete sanitizedAppState[field];
              }
            });
            
            // Call onSave - handle both sync and async callbacks
            try {
              const saveResult = onSave({
                elements: updatedElements,
                appState: sanitizedAppState,
                files: updatedFiles,
              });
              
              // If onSave returns a Promise, handle errors
              if (saveResult && typeof saveResult === 'object' && 'then' in saveResult) {
                (saveResult as Promise<void>).catch((error) => {
                  console.error('Error saving whiteboard data:', error);
                });
              }
            } catch (error) {
              console.error('Error calling onSave:', error);
            }
          } catch (error) {
            console.error('Error saving whiteboard data:', error);
          }
        }, 500);
      }
    },
    [onSave, isReady, hasLoadedInitialData]
  );

  // Handle keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Ctrl/Cmd + S: Save
      if ((event.ctrlKey || event.metaKey) && event.key === 's') {
        event.preventDefault();
        if (onSave && excalidrawAPI) {
          const data = excalidrawAPI.getSceneElements();
          onSave({
            elements: data,
            appState: appState,
            files: files,
          });
        }
      }

      // Ctrl/Cmd + E: Export
      if ((event.ctrlKey || event.metaKey) && event.key === 'e') {
        event.preventDefault();
        if (onExport && excalidrawAPI) {
          const data = excalidrawAPI.getSceneElements();
          onExport({
            elements: data,
            appState: appState,
            files: files,
          });
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [excalidrawAPI, appState, files, onSave, onExport]);

  // Clear canvas handler
  const handleClearCanvas = useCallback(() => {
    if (excalidrawAPI) {
      excalidrawAPI.updateScene({
        elements: [],
      });
    }
  }, [excalidrawAPI]);

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (saveTimeoutRef.current !== null) {
        window.clearTimeout(saveTimeoutRef.current);
      }
    };
  }, []);

  // Don't pass initialData prop - instead use API to load data after mount
  // This avoids React error 185 and state structure issues
  // Only enable onChange after component is fully initialized to prevent infinite loops
  // Multi-select is enabled by default in Excalidraw: Hold Shift and click, or drag to select multiple elements
  try {
    return (
      <div className="whiteboard-container" style={{ height: '100%', width: '100%', display: 'flex', flexDirection: 'column', position: 'relative' }}>
        <Excalidraw
          excalidrawAPI={(api: ExcalidrawImperativeAPI | null) => {
            if (api && !excalidrawAPI) {
              // Use setTimeout to avoid state updates during render
              setTimeout(() => {
                setExcalidrawAPI(api);
              }, 0);
            }
          }}
          onChange={handleChange}
          gridModeEnabled={gridEnabled}
          UIOptions={{
            canvasActions: {
              saveToActiveFile: !readOnly,
              loadScene: !readOnly,
              export: !readOnly ? {} : false,
              clearCanvas: !readOnly,
            },
            tools: {
              image: true,
            },
          }}
          // Enable multi-select by default - users can:
          // - Hold Shift and click to select multiple elements
          // - Drag to create a selection box
          // - Use Ctrl/Cmd+A to select all
          renderTopRightUI={() => (
            <div className="whiteboard-controls" style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
              {!readOnly && (
                <button
                  onClick={handleClearCanvas}
                  className="whiteboard-button"
                  style={{
                    padding: '8px 16px',
                    borderRadius: '4px',
                    border: '1px solid #ddd',
                    backgroundColor: '#fff',
                    cursor: 'pointer',
                    fontSize: '14px',
                  }}
                  title="Clear Canvas"
                >
                  Clear
                </button>
              )}
            </div>
          )}
        />
      </div>
    );
  } catch (error) {
    console.error('Whiteboard rendering error:', error);
    return (
      <div style={{ padding: '20px', textAlign: 'center', height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <div>
          <h2>Whiteboard Error</h2>
          <p>Failed to render whiteboard: {error instanceof Error ? error.message : String(error)}</p>
        </div>
      </div>
    );
  }
};
