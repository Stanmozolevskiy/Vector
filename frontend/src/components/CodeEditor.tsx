import { useRef } from 'react';
import Editor from '@monaco-editor/react';

export interface CodeEditorProps {
  value: string;
  language: string;
  onChange: (value: string | undefined) => void;
  height?: string;
  theme?: 'vs-dark' | 'light';
  readOnly?: boolean;
}

export const CodeEditor = ({
  value,
  language,
  onChange,
  height = '100%',
  theme = 'vs-dark',
  readOnly = false,
}: CodeEditorProps) => {
  const editorRef = useRef<any>(null);

  const handleEditorDidMount = (editor: any) => {
    editorRef.current = editor;
    
    // Configure editor options
    editor.updateOptions({
      fontSize: 14,
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      automaticLayout: true,
      tabSize: 2,
      wordWrap: 'on',
      lineNumbers: 'on',
      renderLineHighlight: 'all',
      selectOnLineNumbers: true,
      roundedSelection: false,
      readOnly: readOnly,
      cursorStyle: 'line',
      fontFamily: 'Consolas, Monaco, "Courier New", monospace',
    });
  };

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

  return (
    <Editor
      height={height}
      language={getLanguageId(language)}
      value={value}
      theme={theme}
      onChange={onChange}
      onMount={handleEditorDidMount}
      options={{
        readOnly: readOnly,
        automaticLayout: true,
      }}
    />
  );
};

