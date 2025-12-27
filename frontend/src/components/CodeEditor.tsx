import { useRef, useCallback, useEffect } from 'react';
import Editor from '@monaco-editor/react';
import { applyLeetCodeStyle, normalizeIndentation } from '../utils/editorStyle';

export interface CodeEditorProps {
  value: string;
  language: string;
  onChange: (value: string | undefined) => void;
  height?: string;
  theme?: 'vs-dark' | 'light'; // Always uses 'light' internally, kept for API compatibility
  readOnly?: boolean;
  errorMarkers?: Array<{
    line: number;
    column?: number;
    endColumn?: number;
    message: string;
  }>;
  onCursorChange?: (line: number, column: number) => void; // Callback for cursor position changes
  onSelectionChange?: (selection: { startLine: number; startColumn: number; endLine: number; endColumn: number } | null) => void; // Callback for selection changes
  remoteCursors?: Array<{ userId: string; line: number; column: number; color: string }>; // Remote user cursors
  remoteSelections?: Array<{ userId: string; startLine: number; startColumn: number; endLine: number; endColumn: number; color: string }>; // Remote user selections
  editorRef?: React.MutableRefObject<any>; // Optional ref to expose editor instance
}

export const CodeEditor = ({
  value,
  language,
  onChange,
  height = '100%',
  theme: _theme = 'light', // Always uses light theme internally
  readOnly: _readOnly = false, // Unused - we always force readOnly: false
  errorMarkers = [],
  onCursorChange,
  onSelectionChange,
  remoteCursors = [],
  remoteSelections = [],
  editorRef: externalEditorRef,
}: CodeEditorProps) => {
  const internalEditorRef = useRef<any>(null);
  const monacoRef = useRef<any>(null);
  const isInitialMount = useRef(true);
  const decorationIdsRef = useRef<string[]>([]);

  // Memoize onChange handler to prevent re-renders
  const handleChange = useCallback((value: string | undefined) => {
    onChange(value);
  }, [onChange]);

  const handleEditorDidMount = useCallback((editor: any, monacoInstance: any) => {
    internalEditorRef.current = editor;
    monacoRef.current = monacoInstance;
    
    // Expose editor instance via external ref if provided
    if (externalEditorRef) {
      externalEditorRef.current = editor;
    }
    
    // Set light theme IMMEDIATELY to prevent black flash
    monacoInstance.editor.setTheme('leetcode-style-light');
    
    // Apply LeetCode style configuration
    applyLeetCodeStyle(monacoInstance, editor);
    
    // Ensure theme stays light
    const themeName = 'leetcode-style-light';
    monacoInstance.editor.setTheme(themeName);
    
    // Normalize indentation (tabs -> 2 spaces)
    normalizeIndentation(editor);
    
    // Configure error underlines for failed submissions
    configureErrorUnderlines(monacoInstance, editor);
    
    // Ensure F12 opens browser console - don't let Monaco capture it
    // Add event listener directly to DOM element to intercept before Monaco
    const editorDomElement = editor.getDomNode();
    if (editorDomElement) {
      const handleF12 = (e: KeyboardEvent) => {
        // F12 key code is 123
        if (e.key === 'F12' || e.keyCode === 123) {
          // Let the browser handle F12 - don't prevent default
          // Stop propagation to prevent Monaco from handling it
          e.stopPropagation();
          // Explicitly don't call preventDefault() - let browser console open
        }
      };
      
      // Use capture phase to intercept before Monaco's handlers
      editorDomElement.addEventListener('keydown', handleF12, true);
      
      // Also use Monaco's onKeyDown as fallback
      editor.onKeyDown((e: any) => {
        if (e.keyCode === 123 || e.browserEvent?.key === 'F12') {
          // Don't prevent default - let browser handle it
          e.browserEvent?.stopPropagation();
        }
      });
    }
    
    // Only focus on initial mount to prevent re-initialization
    if (isInitialMount.current) {
      isInitialMount.current = false;
      
      // Force focus and ensure editor is ready for input (only once)
      setTimeout(() => {
        try {
          editor.focus();
        } catch (e) {
          console.warn('Failed to focus editor:', e);
        }
      }, 100);
    }

    // Re-apply style after layout changes (resizes can reflow)
    const resizeObserver = new ResizeObserver(() => {
      editor.layout();
    });
    
    if (editorDomElement) {
      resizeObserver.observe(editorDomElement);
    }

    // Ensure model always stays 2-space indent when model changes
    editor.onDidChangeModel(() => {
      applyLeetCodeStyle(monacoInstance, editor);
      normalizeIndentation(editor);
      monacoInstance.editor.setTheme('leetcode-style-light');
      configureErrorUnderlines(monacoInstance, editor);
    });

    // Track cursor position changes
    if (onCursorChange) {
      editor.onDidChangeCursorPosition((e: any) => {
        const line = e.position.lineNumber;
        const column = e.position.column;
        onCursorChange(line, column);
      });
    }

    // Track selection changes
    if (onSelectionChange) {
      editor.onDidChangeCursorSelection((e: any) => {
        const selection = e.selection;
        if (selection && !selection.isEmpty()) {
          onSelectionChange({
            startLine: selection.startLineNumber,
            startColumn: selection.startColumn,
            endLine: selection.endLineNumber,
            endColumn: selection.endColumn,
          });
        } else {
          onSelectionChange(null);
        }
      });
    }
  }, [onCursorChange, onSelectionChange]);

  // Configure error underlines in Monaco Editor
  const configureErrorUnderlines = useCallback((monaco: any, editor: any) => {
    if (!monaco || !editor) return;

    const model = editor.getModel();
    if (!model) return;

    let decorationIds: string[] = [];

    // Listen for marker changes (errors/warnings)
    const updateErrorUnderlines = () => {
      const markers = monaco.editor.getModelMarkers({ resource: model.uri });
      
      // Clear existing decorations
      decorationIds = editor.deltaDecorations(decorationIds, []);
      
      // Create underline decorations for errors
      const decorations = markers
        .filter((marker: any) => marker.severity === monaco.MarkerSeverity.Error)
        .map((marker: any) => ({
          range: new monaco.Range(
            marker.startLineNumber,
            marker.startColumn,
            marker.endLineNumber,
            marker.endColumn
          ),
          options: {
            className: 'monaco-error-underline',
            hoverMessage: { value: marker.message },
            glyphMarginClassName: 'monaco-error-glyph',
            overviewRuler: {
              color: '#ef4444',
              position: monaco.editor.OverviewRulerLane.Right,
            },
            minimap: {
              color: '#ef4444',
              position: monaco.editor.MinimapPosition.Inline,
            },
            // Add underline decoration - use inlineClassName but ensure it doesn't block selection
            inlineClassName: 'monaco-error-underline-inline',
            isWholeLine: false,
            // Ensure selection is not blocked
            stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
          },
        }));

      if (decorations.length > 0) {
        decorationIds = editor.deltaDecorations(decorationIds, decorations);
      }
    };

    // Update on marker changes
    const disposable = monaco.editor.onDidChangeMarkers((uris: any[]) => {
      if (uris.some((uri: any) => uri.toString() === model.uri.toString())) {
        updateErrorUnderlines();
      }
    });

    // Initial update
    updateErrorUnderlines();

    // Cleanup on dispose
    return disposable;
  }, []);

  // Re-apply style when language changes (always keep light theme)
  useEffect(() => {
    if (internalEditorRef.current && monacoRef.current) {
      // Set light theme immediately to prevent flash
      monacoRef.current.editor.setTheme('leetcode-style-light');
      applyLeetCodeStyle(monacoRef.current, internalEditorRef.current);
      normalizeIndentation(internalEditorRef.current);
      // Ensure theme stays light
      monacoRef.current.editor.setTheme('leetcode-style-light');
    }
  }, [language]);

  // Add error markers when submission fails
  useEffect(() => {
    if (!internalEditorRef.current || !monacoRef.current) {
      return;
    }

    const model = internalEditorRef.current.getModel();
    if (!model) {
      return;
    }

    if (errorMarkers.length === 0) {
      // Clear markers if no errors
      monacoRef.current.editor.setModelMarkers(model, 'custom-errors', []);
      return;
    }

    // Create markers from errorMarkers prop
    const markers = errorMarkers.map((error) => {
      const lineLength = model.getLineLength(error.line) || 1;
      return {
        startLineNumber: error.line,
        startColumn: error.column || 1,
        endLineNumber: error.line,
        endColumn: error.endColumn || lineLength + 1,
        message: error.message,
        severity: monacoRef.current.MarkerSeverity.Error,
        source: 'Runtime Error',
      };
    });
    
    // Set markers
    monacoRef.current.editor.setModelMarkers(model, 'custom-errors', markers);

    // Trigger underline update
    setTimeout(() => {
      if (internalEditorRef.current && monacoRef.current) {
        configureErrorUnderlines(monacoRef.current, internalEditorRef.current);
      }
    }, 100);
  }, [errorMarkers, configureErrorUnderlines]);

  // Render remote cursors and selections using Monaco decorations
  useEffect(() => {
    if (!internalEditorRef.current || !monacoRef.current) {
      return;
    }

    const editor = internalEditorRef.current;
    const monaco = monacoRef.current;
    const model = editor.getModel();
    if (!model) return;

    // Create cursor decorations with color
    const cursorDecorations = remoteCursors.map((cursor) => {
      const colorClass = cursor.color.replace('#', '');
      return {
        range: new monaco.Range(cursor.line, cursor.column, cursor.line, cursor.column),
        options: {
          className: 'remote-cursor-decoration',
          after: {
            content: ' ',
            inlineClassName: `remote-cursor-after cursor-color-${colorClass}`,
            inlineClassNameAffectsLetterSpacing: true,
          },
          stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        },
      };
    });

    // Create selection decorations with color
    const selectionDecorations = remoteSelections.map((selection) => {
      const colorClass = selection.color.replace('#', '');
      return {
        range: new monaco.Range(
          selection.startLine,
          selection.startColumn,
          selection.endLine,
          selection.endColumn
        ),
        options: {
          className: `remote-selection-decoration selection-color-${colorClass}`,
          hoverMessage: { value: 'Remote selection' },
          stickiness: monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        },
      };
    });

    // Apply all decorations
    decorationIdsRef.current = editor.deltaDecorations(decorationIdsRef.current, [...cursorDecorations, ...selectionDecorations]);

    return () => {
      // Cleanup decorations
      if (decorationIdsRef.current.length > 0) {
        editor.deltaDecorations(decorationIdsRef.current, []);
        decorationIdsRef.current = [];
      }
    };
  }, [remoteCursors, remoteSelections]);

  const getLanguageId = (lang: string): string => {
    const languageMap: Record<string, string> = {
      javascript: 'javascript',
      python: 'python',
      java: 'java',
      cpp: 'cpp',
      csharp: 'csharp',
      go: 'go',
    };
    return languageMap[lang.toLowerCase()] || 'plaintext';
  };

  // Use language as key to force remount when language changes
  return (
    <Editor
      key={language}
      height={height}
      language={getLanguageId(language)}
      value={value}
      theme="light"
      onChange={handleChange}
      onMount={handleEditorDidMount}
      options={{
        readOnly: false, // Always false - we handle readOnly in onMount
        automaticLayout: true,
        domReadOnly: false,
        // Selection behavior
        selectOnLineNumbers: true,
        selectionClipboard: true,
        // Disable line highlight (no horizontal line on selection)
        renderLineHighlight: 'none',
        // Show whitespace dots immediately on selection
        renderWhitespace: 'selection',
        // Word highlighting - highlight related words when clicking
        occurrencesHighlight: 'singleFile',
        selectionHighlight: true,
        // Guides - hide horizontal lines
        guides: {
          bracketPairsHorizontal: false, // Hide horizontal lines in bracket pairs
        },
      }}
    />
  );
};

