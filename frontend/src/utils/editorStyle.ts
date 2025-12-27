/**
 * LeetCode-style Monaco Editor configuration
 * Matches LeetCode's editor appearance and behavior
 */

export function applyLeetCodeStyle(monaco: any, editor: any) {
  // Configure custom theme for JSDoc highlighting
  configureJSDocTheme(monaco);

  // A) Monaco options (behavior + spacing)
  editor.updateOptions({
    // Font/spacing
    fontFamily: `Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace`,
    fontSize: 14,
    lineHeight: 21, // key: fixes "wrong format" look
    letterSpacing: 0,
    fontLigatures: false,

    // Indentation
    tabSize: 4,
    insertSpaces: true,
    detectIndentation: false, // do NOT guess indent from file
    autoIndent: 'advanced',
    formatOnPaste: false, // LeetCode typically doesn't auto-reformat unless asked
    formatOnType: false,

    // Layout
    padding: { top: 10, bottom: 10 },
    scrollBeyondLastLine: false,
    minimap: { enabled: false },
    wordWrap: 'off',

    // Visuals similar to LC
    lineNumbers: 'on',
    renderLineHighlight: 'none', // Disable line highlight (no horizontal line on selection)
    renderWhitespace: 'selection', // Show dots on selected whitespace
    // Only show indent guides for actual scopes (functions, loops with {})
    guides: {
      indentation: false, // Disable general indentation guides
      highlightActiveIndentation: true, // Only highlight active indentation
      bracketPairs: true, // Show bracket pairs but hide horizontal lines
      bracketPairsHorizontal: false, // Hide horizontal lines in bracket pairs
    },
    bracketPairColorization: { enabled: true },
    smoothScrolling: true,
    cursorBlinking: 'blink',

    // Avoid odd spacing artifacts
    fixedOverflowWidgets: true,
    occurrencesHighlight: 'singleFile',
    selectionHighlight: true,

    // Ensure editability
    readOnly: false,
    domReadOnly: false,
    contextmenu: true,
    quickSuggestions: true,
    automaticLayout: true,
  });

  // B) Enforce 4-space indentation at the model level (important)
  const model = editor.getModel?.();
  if (model) {
    model.updateOptions({
      tabSize: 4,
      insertSpaces: true,
      trimAutoWhitespace: true,
    });
  }

  // C) Ensure language-specific settings (JS / TS)
  if (monaco?.languages?.typescript?.javascriptDefaults) {
    monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
      noSemanticValidation: false,
      noSyntaxValidation: false,
    });
  }
}

/**
 * Configure custom theme for JSDoc parameter highlighting
 */
function configureJSDocTheme(monaco: any) {
  if (!monaco) return;

  // Define custom theme for JSDoc
  monaco.editor.defineTheme('leetcode-style', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      // JSDoc comment block (green)
      { token: 'comment.doc', foreground: '6a9955' },
      // JSDoc tags like @param, @return (light blue)
      { token: 'keyword.doc', foreground: '569cd6' },
      // JSDoc parameter types like {number[]}, {number} (yellow/orange)
      { token: 'type.doc', foreground: 'dcdcaa' },
      // JSDoc parameter names like nums, target (cyan)
      { token: 'variable.doc', foreground: '9cdcfe' },
      // Regular comments
      { token: 'comment', foreground: '6a9955' },
      // Keywords
      { token: 'keyword', foreground: '569cd6' },
      // Strings
      { token: 'string', foreground: 'ce9178' },
      // Numbers
      { token: 'number', foreground: 'b5cea8' },
    ],
    colors: {
      'editor.background': '#1e1e1e',
      'editor.foreground': '#d4d4d4',
    },
  });

  // Also configure light theme
  monaco.editor.defineTheme('leetcode-style-light', {
    base: 'vs',
    inherit: true,
    rules: [
      // JSDoc comment block (green)
      { token: 'comment.doc', foreground: '008000' },
      // JSDoc tags like @param, @return (blue)
      { token: 'keyword.doc', foreground: '0000ff' },
      // JSDoc parameter types like {number[]}, {number} (purple)
      { token: 'type.doc', foreground: '795e26' },
      // JSDoc parameter names like nums, target (dark blue)
      { token: 'variable.doc', foreground: '001080' },
      // Regular comments
      { token: 'comment', foreground: '008000' },
      // Keywords
      { token: 'keyword', foreground: '0000ff' },
      // Strings
      { token: 'string', foreground: 'a31515' },
      // Numbers
      { token: 'number', foreground: '098658' },
    ],
    colors: {
      'editor.background': '#ffffff',
      'editor.foreground': '#000000',
      // Selection colors - LeetCode style (light blue)
      'editor.selectionBackground': '#add6ff',
      'editor.selectionForeground': '#000000',
      'editor.inactiveSelectionBackground': '#e5ebf1',
      // Disable line highlight (no horizontal line)
      'editor.lineHighlightBackground': 'transparent',
      'editor.lineHighlightBorder': 'transparent',
      // Word highlighting - highlight related words when clicking
      'editor.wordHighlightBackground': '#add6ff80', // Light blue for word occurrences
      'editor.wordHighlightStrongBackground': '#add6ff80', // Same for strong highlights
      // Whitespace dots color
      'editorWhitespace.foreground': '#d3d3d3', // Light gray for whitespace dots
    },
  });
}

/**
 * Normalize indentation (tabs -> 4 spaces)
 */
export function normalizeIndentation(editor: any) {
  const model = editor.getModel?.();
  if (!model) return;

  const text = model.getValue();
  // normalize tabs -> 4 spaces
  const normalized = text.replace(/\t/g, '    ');

  if (normalized !== text) {
    model.pushEditOperations(
      [],
      [{ range: model.getFullModelRange(), text: normalized }],
      () => null
    );
  }
}

