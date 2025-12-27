import { useState } from 'react';
import type { CreateQuestionDto, Example, QuestionTestCase } from '../../services/question.service';

const DIFFICULTIES = ['Easy', 'Medium', 'Hard'];
const QUESTION_TYPES = ['Coding', 'System Design', 'Behavioral'];
const CATEGORIES = [
  'Arrays', 'Strings', 'Trees', 'Graphs', 'Dynamic Programming',
  'Greedy', 'Backtracking', 'Math', 'Bit Manipulation', 'Sorting',
  'Searching', 'Hash Tables', 'Linked Lists', 'Stacks', 'Queues',
  'Heaps', 'System Design', 'Behavioral'
];

interface QuestionFormProps {
  initialData?: CreateQuestionDto;
  testCases?: Omit<QuestionTestCase, 'id'>[];
  onSubmit: (data: CreateQuestionDto, testCases: Omit<QuestionTestCase, 'id'>[]) => Promise<void>;
  onCancel?: () => void;
  submitLabel?: string;
  loading?: boolean;
}

export const QuestionForm = ({
  initialData,
  testCases: initialTestCases = [],
  onSubmit,
  onCancel,
  submitLabel = 'Create Question',
  loading = false,
}: QuestionFormProps) => {
  const [error, setError] = useState('');
  
  const [formData, setFormData] = useState<CreateQuestionDto>({
    title: initialData?.title || '',
    description: initialData?.description || '',
    difficulty: initialData?.difficulty || 'Medium',
    questionType: initialData?.questionType || 'Coding',
    category: initialData?.category || '',
    companyTags: initialData?.companyTags || [],
    tags: initialData?.tags || [],
    constraints: initialData?.constraints || '',
    examples: initialData?.examples || [],
    hints: initialData?.hints || [],
    timeComplexityHint: initialData?.timeComplexityHint || '',
    spaceComplexityHint: initialData?.spaceComplexityHint || '',
  });

  const [testCases, setTestCases] = useState<Omit<QuestionTestCase, 'id'>[]>(initialTestCases);
  const [newTestCase, setNewTestCase] = useState({
    testCaseNumber: initialTestCases.length + 1,
    input: '',
    expectedOutput: '',
    isHidden: false,
    explanation: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Validate required fields
    if (!formData.title || !formData.description || !formData.category) {
      setError('Please fill in all required fields');
      return;
    }

    try {
      await onSubmit(formData, testCases);
    } catch (err: any) {
      setError(err.response?.data?.error || 'Failed to save question');
    }
  };

  const addTestCase = () => {
    if (!newTestCase.input || !newTestCase.expectedOutput) {
      setError('Test case input and expected output are required');
      return;
    }

    setTestCases([...testCases, { ...newTestCase }]);
    setNewTestCase({
      testCaseNumber: testCases.length + 2,
      input: '',
      expectedOutput: '',
      isHidden: false,
      explanation: '',
    });
    setError('');
  };

  const removeTestCase = (index: number) => {
    setTestCases(testCases.filter((_, i) => i !== index));
  };

  const addExample = () => {
    setFormData({
      ...formData,
      examples: [...(formData.examples || []), { input: '', output: '', explanation: '' }],
    });
  };

  const updateExample = (index: number, field: keyof Example, value: string) => {
    const examples = [...(formData.examples || [])];
    examples[index] = { ...examples[index], [field]: value };
    setFormData({ ...formData, examples });
  };

  const removeExample = (index: number) => {
    setFormData({
      ...formData,
      examples: formData.examples?.filter((_, i) => i !== index) || [],
    });
  };

  const addHint = () => {
    setFormData({
      ...formData,
      hints: [...(formData.hints || []), ''],
    });
  };

  const updateHint = (index: number, value: string) => {
    const hints = [...(formData.hints || [])];
    hints[index] = value;
    setFormData({ ...formData, hints });
  };

  const removeHint = (index: number) => {
    setFormData({
      ...formData,
      hints: formData.hints?.filter((_, i) => i !== index) || [],
    });
  };

  return (
    <form onSubmit={handleSubmit} style={{ background: 'white', padding: '2rem', borderRadius: '12px' }}>
      {error && (
        <div style={{ 
          background: '#fee2e2', 
          color: '#991b1b', 
          padding: '1rem', 
          borderRadius: '8px', 
          marginBottom: '1rem' 
        }}>
          {error}
        </div>
      )}

      {/* Basic Information */}
      <div style={{ marginBottom: '2rem' }}>
        <h2 style={{ marginBottom: '1rem' }}>Basic Information</h2>
        
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>
            Title <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="text"
            value={formData.title}
            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            required
            style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px' }}
          />
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>
            Description <span style={{ color: 'red' }}>*</span>
          </label>
          <textarea
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            required
            rows={10}
            style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px', fontFamily: 'inherit' }}
          />
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>
              Difficulty <span style={{ color: 'red' }}>*</span>
            </label>
            <select
              value={formData.difficulty}
              onChange={(e) => setFormData({ ...formData, difficulty: e.target.value })}
              required
              style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px' }}
            >
              {DIFFICULTIES.map(d => (
                <option key={d} value={d}>{d}</option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>
              Question Type <span style={{ color: 'red' }}>*</span>
            </label>
            <select
              value={formData.questionType}
              onChange={(e) => setFormData({ ...formData, questionType: e.target.value })}
              required
              style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px' }}
            >
              {QUESTION_TYPES.map(t => (
                <option key={t} value={t}>{t}</option>
              ))}
            </select>
          </div>

          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>
              Category <span style={{ color: 'red' }}>*</span>
            </label>
            <select
              value={formData.category}
              onChange={(e) => setFormData({ ...formData, category: e.target.value })}
              required
              style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px' }}
            >
              <option value="">Select category</option>
              {CATEGORIES.map(c => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>
        </div>

        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>
            Constraints
          </label>
          <textarea
            value={formData.constraints || ''}
            onChange={(e) => setFormData({ ...formData, constraints: e.target.value })}
            rows={3}
            placeholder="e.g., 1 <= nums.length <= 10^4"
            style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px', fontFamily: 'inherit' }}
          />
        </div>
      </div>

      {/* Examples */}
      <div style={{ marginBottom: '2rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2>Examples</h2>
          <button type="button" onClick={addExample} style={{ padding: '0.5rem 1rem', background: '#6366f1', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer' }}>
            + Add Example
          </button>
        </div>
        {formData.examples?.map((example, idx) => (
          <div key={idx} style={{ marginBottom: '1rem', padding: '1rem', background: '#f9fafb', borderRadius: '8px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
              <strong>Example {idx + 1}</strong>
              <button type="button" onClick={() => removeExample(idx)} style={{ color: 'red', background: 'none', border: 'none', cursor: 'pointer' }}>
                Remove
              </button>
            </div>
            <div style={{ marginBottom: '0.5rem' }}>
              <label style={{ display: 'block', marginBottom: '0.25rem' }}>Input:</label>
              <input
                type="text"
                value={example.input || ''}
                onChange={(e) => updateExample(idx, 'input', e.target.value)}
                style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px' }}
              />
            </div>
            <div style={{ marginBottom: '0.5rem' }}>
              <label style={{ display: 'block', marginBottom: '0.25rem' }}>Output:</label>
              <input
                type="text"
                value={example.output || ''}
                onChange={(e) => updateExample(idx, 'output', e.target.value)}
                style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px' }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: '0.25rem' }}>Explanation:</label>
              <textarea
                value={example.explanation || ''}
                onChange={(e) => updateExample(idx, 'explanation', e.target.value)}
                rows={2}
                style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px', fontFamily: 'inherit' }}
              />
            </div>
          </div>
        ))}
      </div>

      {/* Test Cases */}
      <div style={{ marginBottom: '2rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2>Test Cases (Judge0 Compatible)</h2>
        </div>
        <p style={{ color: '#6b7280', marginBottom: '1rem', fontSize: '0.875rem' }}>
          Test cases should be in plain string format for Judge0 compatibility. Input will be used as stdin, expected output will be compared.
        </p>
        
        {testCases.map((testCase, idx) => (
          <div key={idx} style={{ marginBottom: '1rem', padding: '1rem', background: '#f9fafb', borderRadius: '8px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
              <strong>Test Case {testCase.testCaseNumber} {testCase.isHidden && '(Hidden)'}</strong>
              <button type="button" onClick={() => removeTestCase(idx)} style={{ color: 'red', background: 'none', border: 'none', cursor: 'pointer' }}>
                Remove
              </button>
            </div>
            <div style={{ marginBottom: '0.5rem' }}>
              <label style={{ display: 'block', marginBottom: '0.25rem' }}>Input (stdin format):</label>
              <textarea
                value={testCase.input}
                readOnly
                rows={2}
                style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px', fontFamily: 'monospace', background: 'white' }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: '0.25rem' }}>Expected Output:</label>
              <textarea
                value={testCase.expectedOutput}
                readOnly
                rows={2}
                style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px', fontFamily: 'monospace', background: 'white' }}
              />
            </div>
          </div>
        ))}

        {/* New Test Case Form */}
        <div style={{ padding: '1rem', background: '#f0f9ff', borderRadius: '8px', border: '2px dashed #3b82f6' }}>
          <h3 style={{ marginBottom: '1rem' }}>New Test Case</h3>
          <div style={{ marginBottom: '0.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 600 }}>
              Input (stdin format) <span style={{ color: 'red' }}>*</span>
            </label>
            <textarea
              value={newTestCase.input}
              onChange={(e) => setNewTestCase({ ...newTestCase, input: e.target.value })}
              placeholder="e.g., [2, 7, 11, 15]&#10;9"
              rows={3}
              style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px', fontFamily: 'monospace' }}
            />
            <small style={{ color: '#6b7280' }}>Plain text format that will be passed as stdin to Judge0</small>
          </div>
          <div style={{ marginBottom: '0.5rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: 600 }}>
              Expected Output <span style={{ color: 'red' }}>*</span>
            </label>
            <textarea
              value={newTestCase.expectedOutput}
              onChange={(e) => setNewTestCase({ ...newTestCase, expectedOutput: e.target.value })}
              placeholder="e.g., [0, 1]"
              rows={2}
              style={{ width: '100%', padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px', fontFamily: 'monospace' }}
            />
            <small style={{ color: '#6b7280' }}>Plain text format that Judge0 will compare against</small>
          </div>
          <div style={{ marginBottom: '0.5rem' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
              <input
                type="checkbox"
                checked={newTestCase.isHidden}
                onChange={(e) => setNewTestCase({ ...newTestCase, isHidden: e.target.checked })}
              />
              <span>Hidden test case (not shown to users before submission)</span>
            </label>
          </div>
          <button
            type="button"
            onClick={addTestCase}
            style={{ padding: '0.5rem 1rem', background: '#6366f1', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
          >
            Add Test Case
          </button>
        </div>
      </div>

      {/* Hints */}
      <div style={{ marginBottom: '2rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
          <h2>Hints</h2>
          <button type="button" onClick={addHint} style={{ padding: '0.5rem 1rem', background: '#6366f1', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer' }}>
            + Add Hint
          </button>
        </div>
        {formData.hints?.map((hint, idx) => (
          <div key={idx} style={{ marginBottom: '0.5rem', display: 'flex', gap: '0.5rem' }}>
            <input
              type="text"
              value={hint}
              onChange={(e) => updateHint(idx, e.target.value)}
              placeholder={`Hint ${idx + 1}`}
              style={{ flex: 1, padding: '0.5rem', border: '1px solid #e5e7eb', borderRadius: '4px' }}
            />
            <button type="button" onClick={() => removeHint(idx)} style={{ padding: '0.5rem 1rem', color: 'red', background: 'none', border: '1px solid red', borderRadius: '4px', cursor: 'pointer' }}>
              Remove
            </button>
          </div>
        ))}
      </div>

      {/* Complexity Hints */}
      <div style={{ marginBottom: '2rem' }}>
        <h2 style={{ marginBottom: '1rem' }}>Complexity Hints</h2>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>Time Complexity Hint</label>
            <input
              type="text"
              value={formData.timeComplexityHint || ''}
              onChange={(e) => setFormData({ ...formData, timeComplexityHint: e.target.value })}
              placeholder="e.g., O(n)"
              style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px' }}
            />
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 600 }}>Space Complexity Hint</label>
            <input
              type="text"
              value={formData.spaceComplexityHint || ''}
              onChange={(e) => setFormData({ ...formData, spaceComplexityHint: e.target.value })}
              placeholder="e.g., O(1)"
              style={{ width: '100%', padding: '0.75rem', border: '1px solid #e5e7eb', borderRadius: '6px' }}
            />
          </div>
        </div>
      </div>

      {/* Submit Buttons */}
      <div style={{ display: 'flex', gap: '1rem', justifyContent: 'flex-end' }}>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            style={{ padding: '0.75rem 1.5rem', background: 'white', color: '#374151', border: '1px solid #e5e7eb', borderRadius: '6px', cursor: 'pointer' }}
          >
            Cancel
          </button>
        )}
        <button
          type="submit"
          disabled={loading}
          style={{ padding: '0.75rem 1.5rem', background: '#6366f1', color: 'white', border: 'none', borderRadius: '6px', cursor: loading ? 'not-allowed' : 'pointer', opacity: loading ? 0.6 : 1 }}
        >
          {loading ? 'Saving...' : submitLabel}
        </button>
      </div>
    </form>
  );
};

