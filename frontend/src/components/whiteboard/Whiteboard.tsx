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
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [excalidrawAPI, setExcalidrawAPI] = useState<ExcalidrawImperativeAPI | null>(null);
  const [appState, setAppState] = useState<any>({});
  const [files, setFiles] = useState<Record<string, any>>({});
  const [hasLoadedInitialData, setHasLoadedInitialData] = useState(false);
  const [isReady, setIsReady] = useState(false);
  const saveTimeoutRef = useRef<number | null>(null);
  const isUpdatingFromPropRef = useRef<boolean>(false);
  const remoteApplyResetTimeoutRef = useRef<number | null>(null);
  const lastAppliedRemoteSignatureRef = useRef<string>('');
  const resizeRafRef = useRef<number | null>(null);

  const getElementsSignature = useCallback((elements: readonly any[]) => {
    // Lightweight signature to detect "remote apply caused onChange" without expensive deep stringifies.
    // Excalidraw elements have stable `id` and incrementing `version`.
    try {
      const parts = elements.map((el: any) => `${el?.id ?? ''}:${el?.version ?? ''}`);
      return `${elements.length}|${parts.join(',')}`;
    } catch {
      return `${elements.length}`;
    }
  }, []);

  // Track previous initialData to detect changes
  const prevInitialDataRef = useRef<ExcalidrawInitialDataState | undefined>(initialData);

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
      }, 150);
      
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

  // Ensure Excalidraw reflows correctly when its container size changes
  // (e.g. sidebar open/close transitions). This prevents "right-side gaps" where the canvas
  // doesn't resize until a hard refresh.
  useEffect(() => {
    if (!excalidrawAPI) return;
    const node = containerRef.current;
    if (!node || typeof ResizeObserver === 'undefined') return;

    const handleResize = () => {
      if (resizeRafRef.current != null) {
        cancelAnimationFrame(resizeRafRef.current);
      }

      resizeRafRef.current = requestAnimationFrame(() => {
        resizeRafRef.current = null;

        // Force Excalidraw to re-measure by re-applying the current elements.
        // Guard with isUpdatingFromPropRef to avoid triggering saves/broadcasts.
        isUpdatingFromPropRef.current = true;
        try {
          excalidrawAPI.updateScene({
            elements: excalidrawAPI.getSceneElements(),
          });
        } catch {
          // ignore
        } finally {
          // Short quiet window; we just want to suppress the immediate onChange after updateScene.
          setTimeout(() => {
            isUpdatingFromPropRef.current = false;
          }, 50);
        }
      });
    };

    const ro = new ResizeObserver(handleResize);
    ro.observe(node);

    return () => {
      ro.disconnect();
      if (resizeRafRef.current != null) {
        cancelAnimationFrame(resizeRafRef.current);
        resizeRafRef.current = null;
      }
    };
  }, [excalidrawAPI]);

  // Handle updates to initialData prop (for real-time collaboration)
  useEffect(() => {
    if (excalidrawAPI && initialData && hasLoadedInitialData && isReady) {
      // Check if initialData actually changed (to avoid infinite loops)
      const prevData = prevInitialDataRef.current;
      
      // Compare elements more reliably - use a deep comparison
      const currentElements = initialData.elements || [];
      const prevElements = prevData?.elements || [];
      
      // Always check if elements actually changed (deep comparison)
      const elementsChanged = JSON.stringify(currentElements) !== JSON.stringify(prevElements);
      
      if (elementsChanged) {
        // Apply the newest remote elements immediately.
        // Keep the "updating from prop" flag true until we've had a quiet period,
        // otherwise rapid remote updates can briefly drop the flag and create a save/broadcast loop (causes blinking).
        isUpdatingFromPropRef.current = true;
        if (remoteApplyResetTimeoutRef.current !== null) {
          window.clearTimeout(remoteApplyResetTimeoutRef.current);
          remoteApplyResetTimeoutRef.current = null;
        }

        try {
          if (Array.isArray(currentElements)) {
            lastAppliedRemoteSignatureRef.current = getElementsSignature(currentElements);
            // Only sync elements from props. Do NOT sync appState (it includes activeTool etc),
            // otherwise remote updates can override the local user's current tool selection.
            excalidrawAPI.updateScene({
              elements: currentElements,
            });
          }
        } catch {
          // Silent fail - errors are handled by error boundary
        } finally {
          // Reset flag only after remote updates calm down
          remoteApplyResetTimeoutRef.current = window.setTimeout(() => {
            isUpdatingFromPropRef.current = false;
            remoteApplyResetTimeoutRef.current = null;
          }, 250);
        }
      }
      
      prevInitialDataRef.current = initialData;
    }
  }, [excalidrawAPI, initialData, hasLoadedInitialData, isReady]);

  // Handle changes to the whiteboard - debounce saves to prevent infinite loops
  // Always provide a function (not conditionally undefined) to avoid re-renders
  const handleChange = useCallback(
    (updatedElements: readonly any[], updatedAppState: any, updatedFiles: Record<string, any>) => {
      // Always update local state
      setAppState(updatedAppState);
      setFiles(updatedFiles);

      // If this change matches the last remote scene we applied, never propagate it back out.
      // This avoids feedback loops that can cause blinking (especially with frequent updates like freehand).
      if (Array.isArray(updatedElements)) {
        const signature = getElementsSignature(updatedElements);
        if (signature === lastAppliedRemoteSignatureRef.current) {
          return;
        }
      }

      // Only save if component is ready and this isn't during initial load
      // Skip save if we're updating from prop (to prevent loops)
      // Use debouncing to prevent rapid successive saves and infinite loops
      if (onSave && isReady && hasLoadedInitialData && !isUpdatingFromPropRef.current) {
        // Clear existing timeout
        if (saveTimeoutRef.current !== null) {
          window.clearTimeout(saveTimeoutRef.current);
        }
        
        // Faster debounce for better realtime collaboration smoothness
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
            }, 120);
      }
    },
    [onSave, isReady, hasLoadedInitialData, getElementsSignature]
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
      if (remoteApplyResetTimeoutRef.current !== null) {
        window.clearTimeout(remoteApplyResetTimeoutRef.current);
      }
    };
  }, []);

  // Don't pass initialData prop - instead use API to load data after mount
  // This avoids React error 185 and state structure issues
  // Only enable onChange after component is fully initialized to prevent infinite loops
  // Multi-select is enabled by default in Excalidraw: Hold Shift and click, or drag to select multiple elements
  try {
    return (
      <div
        ref={containerRef}
        className="whiteboard-container"
        style={{ height: '100%', width: '100%', display: 'flex', flexDirection: 'column', position: 'relative' }}
      >
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
