import { useRef } from 'react';

type MarkdownEditorProps = {
  value: string;
  onChange: (next: string) => void;
  placeholder?: string;
  rows?: number;
  ariaLabel?: string;
  className?: string;
};

type WrapConfig = {
  before: string;
  after: string;
  defaultText?: string;
};

const wrapSelection = (textarea: HTMLTextAreaElement, config: WrapConfig) => {
  const value = textarea.value;
  const start = textarea.selectionStart ?? 0;
  const end = textarea.selectionEnd ?? 0;
  const selected = value.slice(start, end) || config.defaultText || '';
  const next = value.slice(0, start) + config.before + selected + config.after + value.slice(end);

  const caretStart = start + config.before.length;
  const caretEnd = caretStart + selected.length;
  return { next, caretStart, caretEnd };
};

const prefixLines = (textarea: HTMLTextAreaElement, prefix: string) => {
  const value = textarea.value;
  const start = textarea.selectionStart ?? 0;
  const end = textarea.selectionEnd ?? 0;
  const selected = value.slice(start, end) || '';

  const lines = (selected || ' ').split('\n').map((l) => (l.trim() ? `${prefix}${l}` : l));
  const transformed = lines.join('\n');
  const next = value.slice(0, start) + transformed + value.slice(end);
  return { next, caretStart: start, caretEnd: start + transformed.length };
};

export const MarkdownEditor = ({
  value,
  onChange,
  placeholder,
  rows = 6,
  ariaLabel,
  className,
}: MarkdownEditorProps) => {
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const apply = (fn: (el: HTMLTextAreaElement) => { next: string; caretStart: number; caretEnd: number }) => {
    const el = textareaRef.current;
    if (!el) return;
    const res = fn(el);
    onChange(res.next);
    requestAnimationFrame(() => {
      el.focus();
      el.setSelectionRange(res.caretStart, res.caretEnd);
    });
  };

  const handleBold = () => apply((el) => wrapSelection(el, { before: '**', after: '**', defaultText: 'bold' }));
  const handleItalic = () => apply((el) => wrapSelection(el, { before: '*', after: '*', defaultText: 'italic' }));
  const handleInlineCode = () => apply((el) => wrapSelection(el, { before: '`', after: '`', defaultText: 'code' }));
  const handleCodeBlock = () =>
    apply((el) => wrapSelection(el, { before: '\n```text\n', after: '\n```\n', defaultText: '' }));
  const handleUnorderedList = () => apply((el) => prefixLines(el, '- '));
  const handleOrderedList = () => apply((el) => prefixLines(el, '1. '));
  const handleBlockquote = () => apply((el) => prefixLines(el, '> '));

  return (
    <div className={className}>
      <textarea
        ref={textareaRef}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        rows={rows}
        aria-label={ariaLabel}
        className="qa-editor-textarea"
      />

      <div className="qa-editor-toolbar" aria-label="Formatting toolbar">
        <button type="button" className="qa-toolbar-btn" onClick={handleBold} aria-label="Bold">
          <i className="fa-solid fa-bold"></i>
        </button>
        <button type="button" className="qa-toolbar-btn" onClick={handleItalic} aria-label="Italic">
          <i className="fa-solid fa-italic"></i>
        </button>
        <button type="button" className="qa-toolbar-btn" onClick={handleInlineCode} aria-label="Inline code">
          <i className="fa-solid fa-code"></i>
        </button>
        <button type="button" className="qa-toolbar-btn" onClick={handleUnorderedList} aria-label="Bulleted list">
          <i className="fa-solid fa-list-ul"></i>
        </button>
        <button type="button" className="qa-toolbar-btn" onClick={handleOrderedList} aria-label="Numbered list">
          <i className="fa-solid fa-list-ol"></i>
        </button>
        <button type="button" className="qa-toolbar-btn" onClick={handleBlockquote} aria-label="Blockquote">
          <i className="fa-solid fa-quote-right"></i>
        </button>
        <button type="button" className="qa-toolbar-btn" onClick={handleCodeBlock} aria-label="Code block">
          <i className="fa-solid fa-file-code"></i>
        </button>
      </div>
    </div>
  );
};

