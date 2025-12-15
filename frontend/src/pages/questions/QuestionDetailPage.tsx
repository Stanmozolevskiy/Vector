import { useEffect, useState, useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { questionService } from '../../services/question.service';
import type { InterviewQuestion, QuestionTestCase, QuestionSolution } from '../../services/question.service';
import { ROUTES } from '../../utils/constants';
import { CodeEditor } from '../../components/CodeEditor';
import { useAuth } from '../../hooks/useAuth';
import '../../styles/question-detail.css';

const LANGUAGE_TEMPLATES: Record<string, string> = {
  javascript: '/**\n * @param {number[]} nums\n * @param {number} target\n * @return {number[]}\n */\nvar twoSum = function(nums, target) {\n    \n};',
  python: 'def twoSum(nums, target):\n    # Write your code here\n    pass',
  java: 'class Solution {\n    public int[] twoSum(int[] nums, int target) {\n        // Write your code here\n        \n    }\n}',
  cpp: 'class Solution {\npublic:\n    vector<int> twoSum(vector<int>& nums, int target) {\n        // Write your code here\n        \n    }\n};',
  csharp: 'public class Solution {\n    public int[] TwoSum(int[] nums, int target) {\n        // Write your code here\n        \n    }\n}',
  go: 'func twoSum(nums []int, target int) []int {\n    // Write your code here\n    \n}',
};

export const QuestionDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [question, setQuestion] = useState<InterviewQuestion | null>(null);
  const [testCases, setTestCases] = useState<QuestionTestCase[]>([]);
  const [solutions, setSolutions] = useState<QuestionSolution[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('description');
  const [selectedLanguage, setSelectedLanguage] = useState('javascript');
  const [code, setCode] = useState(LANGUAGE_TEMPLATES.javascript);
  const [testInputs, setTestInputs] = useState<Record<number, string>>({});
  const [activeTestTab, setActiveTestTab] = useState<'testcase' | 'result'>('testcase');
  const [testResults, setTestResults] = useState<Array<{
    testCaseNumber: number;
    passed: boolean;
    output?: string;
    error?: string;
    runtime?: number;
    memory?: number;
    status: string;
  }>>([]);
  const [executionResult, setExecutionResult] = useState<{
    status: string;
    runtime?: number;
    memory?: number;
    output?: string;
    error?: string;
  } | null>(null);
  const descriptionPanelRef = useRef<HTMLDivElement>(null);
  const resizerRef = useRef<HTMLDivElement>(null);
  const editorPanelRef = useRef<HTMLDivElement>(null);
  const codeAreaRef = useRef<HTMLDivElement>(null);
  const testcasePanelRef = useRef<HTMLDivElement>(null);
  const horizontalResizerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (id) {
      loadQuestion();
    }
  }, [id]);

  useEffect(() => {
    if (selectedLanguage && LANGUAGE_TEMPLATES[selectedLanguage]) {
      setCode(LANGUAGE_TEMPLATES[selectedLanguage]);
    }
  }, [selectedLanguage]);

  useEffect(() => {
    if (testCases.length > 0) {
      const inputs: Record<number, string> = {};
      testCases.forEach((_, idx) => {
        inputs[idx] = '';
      });
      setTestInputs(inputs);
    }
  }, [testCases]);

  // Vertical resizer (between description and editor)
  useEffect(() => {
    const resizer = resizerRef.current;
    if (!resizer || !question) return;

    let isResizing = false;
    let startX = 0;
    let startWidth = 0;

    const handleMouseDown = (e: MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      isResizing = true;
      startX = e.clientX;
      if (descriptionPanelRef.current) {
        startWidth = descriptionPanelRef.current.offsetWidth;
      }
      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
      document.body.style.pointerEvents = 'none';
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing || !descriptionPanelRef.current || !editorPanelRef.current) return;
      
      e.preventDefault();
      const deltaX = e.clientX - startX;
      const newWidth = startWidth + deltaX;
      const container = document.querySelector('.question-container') as HTMLElement;
      if (!container) return;
      
      const containerWidth = container.offsetWidth;
      const minWidth = 300;
      const maxWidth = containerWidth - 300;
      
      if (newWidth >= minWidth && newWidth <= maxWidth) {
        descriptionPanelRef.current.style.width = `${newWidth}px`;
        descriptionPanelRef.current.style.flexShrink = '0';
        descriptionPanelRef.current.style.flexGrow = '0';
        editorPanelRef.current.style.flex = '1';
      }
    };

    const handleMouseUp = () => {
      if (isResizing) {
        isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        document.body.style.pointerEvents = '';
      }
    };

    resizer.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      resizer.removeEventListener('mousedown', handleMouseDown);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [question]);

  // Horizontal resizer (between code area and testcase panel)
  useEffect(() => {
    const resizer = horizontalResizerRef.current;
    if (!resizer || !question) return;

    let isResizing = false;
    let startY = 0;
    let startHeight = 0;

    const handleMouseDown = (e: MouseEvent) => {
      e.preventDefault();
      e.stopPropagation();
      isResizing = true;
      startY = e.clientY;
      if (codeAreaRef.current) {
        startHeight = codeAreaRef.current.offsetHeight;
      }
      document.body.style.cursor = 'row-resize';
      document.body.style.userSelect = 'none';
      document.body.style.pointerEvents = 'none';
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (!isResizing || !codeAreaRef.current || !testcasePanelRef.current) return;
      
      e.preventDefault();
      const deltaY = e.clientY - startY;
      const newHeight = startHeight + deltaY;
      const editorPanel = editorPanelRef.current;
      if (!editorPanel) return;
      
      const editorHeight = editorPanel.offsetHeight;
      const minHeight = 200;
      const maxHeight = editorHeight - 200;
      
      if (newHeight >= minHeight && newHeight <= maxHeight) {
        codeAreaRef.current.style.height = `${newHeight}px`;
        codeAreaRef.current.style.flex = 'none';
        codeAreaRef.current.style.flexShrink = '0';
        codeAreaRef.current.style.flexGrow = '0';
      }
    };

    const handleMouseUp = () => {
      if (isResizing) {
        isResizing = false;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        document.body.style.pointerEvents = '';
      }
    };

    resizer.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      resizer.removeEventListener('mousedown', handleMouseDown);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [question]);

  const loadQuestion = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const [questionData, testCasesData, solutionsData] = await Promise.all([
        questionService.getQuestionById(id),
        questionService.getTestCases(id, false),
        questionService.getSolutions(id),
      ]);
      setQuestion(questionData);
      setTestCases(testCasesData);
      setSolutions(solutionsData);
    } catch (error) {
      console.error('Failed to load question:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRunCode = async () => {
    if (!question) return;
    
    try {
      // TODO: Implement code execution with Judge0
      // For now, simulate execution
      setExecutionResult({
        status: 'Accepted',
        runtime: 52,
        memory: 42100,
        output: 'Test output',
      });
      setActiveTestTab('result');
    } catch (error) {
      console.error('Failed to run code:', error);
      setExecutionResult({
        status: 'Error',
        error: 'Failed to execute code',
      });
      setActiveTestTab('result');
    }
  };

  const handleSubmit = async () => {
    if (!question) return;
    
    try {
      // TODO: Implement code validation with Judge0
      // For now, simulate test case results
      const results = testCases.map((testCase) => ({
        testCaseNumber: testCase.testCaseNumber,
        passed: Math.random() > 0.3, // Simulate some passing
        output: `Output for test case ${testCase.testCaseNumber}`,
        runtime: Math.floor(Math.random() * 100),
        memory: Math.floor(Math.random() * 50000),
        status: Math.random() > 0.3 ? 'Accepted' : 'Wrong Answer',
      }));
      
      setTestResults(results);
      setActiveTestTab('result');
    } catch (error) {
      console.error('Failed to submit code:', error);
      setTestResults([{
        testCaseNumber: 1,
        passed: false,
        error: 'Failed to validate solution',
        status: 'Error',
      }]);
      setActiveTestTab('result');
    }
  };

  const getDifficultyClass = (difficulty: string) => {
    return difficulty.toLowerCase();
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <i className="fas fa-spinner fa-spin text-4xl text-blue-600 mb-4"></i>
          <p className="text-gray-600">Loading question...</p>
        </div>
      </div>
    );
  }

  if (!question) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-center">
          <h2 className="text-2xl font-bold mb-4">Question not found</h2>
          <Link to={ROUTES.QUESTIONS} className="btn-primary">
            Back to Questions
          </Link>
        </div>
      </div>
    );
  }

  // Only show coding question layout for coding questions
  if (question.questionType?.toLowerCase() !== 'coding') {
    // For non-coding questions, show a simpler layout
    return (
      <div className="question-detail-page">
        <nav className="question-navbar">
          <div className="question-nav-left">
            <Link to={ROUTES.QUESTIONS} className="back-btn">
              <i className="fas fa-chevron-left"></i>
            </Link>
            <div className="question-nav-title">
              <span className="nav-question-number">{question.title}</span>
            </div>
          </div>
        </nav>
        <div className="container" style={{ paddingTop: '70px' }}>
          <h1>{question.title}</h1>
          <div dangerouslySetInnerHTML={{ __html: question.description.replace(/\n/g, '<br />') }} />
        </div>
      </div>
    );
  }

  return (
    <div className="question-detail-page">
      <nav className="question-navbar">
        <div className="question-nav-left">
          <Link to={ROUTES.QUESTIONS} className="back-btn">
            <i className="fas fa-chevron-left"></i>
          </Link>
          <div className="question-nav-title">
            <span className="nav-question-number">{question.title}</span>
          </div>
        </div>
        <div className="question-nav-center">
          <button className="nav-btn" onClick={handleRunCode}>
            <i className="fas fa-play"></i> Run
          </button>
          <button className="nav-btn nav-btn-primary" onClick={handleSubmit}>
            <i className="fas fa-check"></i> Submit
          </button>
        </div>
        <div className="question-nav-right">
          {(user?.role === 'admin' || user?.role === 'coach') && (
            <Link
              to={`${ROUTES.EDIT_QUESTION}/${id}`}
              className="nav-icon-btn"
              title="Edit Question"
            >
              <i className="fas fa-edit"></i>
            </Link>
          )}
          <button className="nav-icon-btn" title="Settings">
            <i className="fas fa-cog"></i>
          </button>
        </div>
      </nav>

      <div className="question-container">
        <div className="description-panel" ref={descriptionPanelRef}>
          <div className="panel-tabs">
            <button
              className={`panel-tab ${activeTab === 'description' ? 'active' : ''}`}
              onClick={() => setActiveTab('description')}
            >
              <i className="fas fa-file-alt"></i> Description
            </button>
            <button
              className={`panel-tab ${activeTab === 'editorial' ? 'active' : ''}`}
              onClick={() => setActiveTab('editorial')}
            >
              <i className="fas fa-lightbulb"></i> Editorial
            </button>
            <button
              className={`panel-tab ${activeTab === 'solutions' ? 'active' : ''}`}
              onClick={() => setActiveTab('solutions')}
            >
              <i className="fas fa-code"></i> Solutions
            </button>
          </div>

          {activeTab === 'description' && (
            <div className="panel-content">
              <div className="question-header">
                <h1 className="question-title">{question.title}</h1>
                <div className="question-meta">
                  <span className={`difficulty-badge ${getDifficultyClass(question.difficulty)}`}>
                    {question.difficulty}
                  </span>
                  {question.tags && question.tags.length > 0 && (
                    <button className="meta-btn">
                      <i className="far fa-bookmark"></i> Topics
                    </button>
                  )}
                  {question.companyTags && question.companyTags.length > 0 && (
                    <button className="meta-btn">
                      <i className="far fa-building"></i> Companies
                    </button>
                  )}
                  {question.hints && question.hints.length > 0 && (
                    <button className="meta-btn">
                      <i className="far fa-lightbulb"></i> Hint 1
                    </button>
                  )}
                </div>
              </div>

              <div className="problem-content">
                <div dangerouslySetInnerHTML={{ __html: question.description.replace(/\n/g, '<br />') }} />
                
                {question.examples && question.examples.length > 0 && (
                  <>
                    {question.examples.map((example, idx) => (
                      <div key={idx} className="example-section">
                        <h3>Example {idx + 1}:</h3>
                        <div className="example-box">
                          {example.input && (
                            <div className="example-line">
                              <strong>Input:</strong> {example.input}
                            </div>
                          )}
                          {example.output && (
                            <div className="example-line">
                              <strong>Output:</strong> {example.output}
                            </div>
                          )}
                          {example.explanation && (
                            <div className="example-line">
                              <strong>Explanation:</strong> {example.explanation}
                            </div>
                          )}
                        </div>
                      </div>
                    ))}
                  </>
                )}

                {question.constraints && (
                  <div className="constraints-section">
                    <h3>Constraints:</h3>
                    <ul className="constraints-list">
                      {question.constraints.split('\n').map((constraint, idx) => (
                        <li key={idx} dangerouslySetInnerHTML={{ __html: constraint }} />
                      ))}
                    </ul>
                  </div>
                )}

                {question.acceptanceRate && (
                  <div className="stats-section">
                    <div className="stat-item">
                      <span className="stat-label">Acceptance Rate</span>
                      <span className="stat-value">{question.acceptanceRate}%</span>
                    </div>
                  </div>
                )}

                {question.tags && question.tags.length > 0 && (
                  <div className="sidebar-links">
                    <div className="sidebar-section">
                      <h4><i className="fas fa-tags"></i> Topics</h4>
                      <div className="topic-tags">
                        {question.tags.map((tag, idx) => (
                          <button key={idx} className="topic-tag">{tag}</button>
                        ))}
                      </div>
                    </div>
                  </div>
                )}

                {question.companyTags && question.companyTags.length > 0 && (
                  <div className="sidebar-links">
                    <div className="sidebar-section">
                      <h4><i className="fas fa-building"></i> Companies</h4>
                      <div className="company-list">
                        {question.companyTags.map((company, idx) => (
                          <button key={idx} className="company-badge">{company}</button>
                        ))}
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          )}

          {activeTab === 'editorial' && (
            <div className="panel-content">
              <h2>Editorial</h2>
              {question.hints && question.hints.length > 0 && (
                <div>
                  <h3>Hints:</h3>
                  <ul>
                    {question.hints.map((hint, idx) => (
                      <li key={idx}>{hint}</li>
                    ))}
                  </ul>
                </div>
              )}
              {question.timeComplexityHint && (
                <p><strong>Time Complexity:</strong> {question.timeComplexityHint}</p>
              )}
              {question.spaceComplexityHint && (
                <p><strong>Space Complexity:</strong> {question.spaceComplexityHint}</p>
              )}
            </div>
          )}

          {activeTab === 'solutions' && solutions.length > 0 && (
            <div className="panel-content">
              <h2>Solutions</h2>
              {solutions.map((solution, idx) => (
                <div key={solution.id} style={{ marginBottom: '2rem' }}>
                  <h3>{idx + 1}. {solution.language} Solution</h3>
                  {solution.explanation && <p>{solution.explanation}</p>}
                  <pre><code>{solution.code}</code></pre>
                  {solution.timeComplexity && (
                    <p><strong>Time Complexity:</strong> {solution.timeComplexity}</p>
                  )}
                  {solution.spaceComplexity && (
                    <p><strong>Space Complexity:</strong> {solution.spaceComplexity}</p>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="panel-resizer" ref={resizerRef}></div>

        <div className="editor-panel" ref={editorPanelRef}>
          <div className="editor-header">
            <div className="editor-controls">
              <select
                className="language-select"
                value={selectedLanguage}
                onChange={(e) => setSelectedLanguage(e.target.value)}
              >
                <option value="javascript">JavaScript</option>
                <option value="python">Python3</option>
                <option value="java">Java</option>
                <option value="cpp">C++</option>
                <option value="csharp">C#</option>
                <option value="go">Go</option>
              </select>
            </div>
          </div>

          <div className="code-area" ref={codeAreaRef}>
            <CodeEditor
              value={code}
              language={selectedLanguage}
              onChange={(value) => setCode(value || '')}
              height="100%"
              theme="vs-dark"
            />
          </div>

          <div className="panel-resizer horizontal-resizer" ref={horizontalResizerRef}></div>

          <div className="testcase-panel" ref={testcasePanelRef}>
            <div className="testcase-header">
              <div className="testcase-tabs">
                <button 
                  className={`testcase-tab ${activeTestTab === 'testcase' ? 'active' : ''}`}
                  onClick={() => setActiveTestTab('testcase')}
                >
                  <i className="fas fa-vial"></i> Testcase
                </button>
                <button 
                  className={`testcase-tab ${activeTestTab === 'result' ? 'active' : ''}`}
                  onClick={() => setActiveTestTab('result')}
                >
                  <i className="fas fa-chart-bar"></i> Test Result
                </button>
              </div>
            </div>
            <div className="testcase-content">
              {activeTestTab === 'testcase' ? (
                <>
                  {testCases.map((testCase, index) => (
                    <div key={testCase.id} className="testcase-input">
                      <div className="input-label">Test Case {testCase.testCaseNumber}:</div>
                      <input
                        type="text"
                        className="testcase-input-field"
                        value={testInputs[index] || ''}
                        onChange={(e) => setTestInputs(prev => ({ ...prev, [index]: e.target.value }))}
                        placeholder={testCase.input}
                      />
                    </div>
                  ))}
                  <div className="testcase-actions">
                    <button className="testcase-btn">
                      <i className="fas fa-plus"></i> Add testcase
                    </button>
                  </div>
                </>
              ) : (
                <div className="test-result-content">
                  {executionResult && (
                    <div className="result-status">
                      <div className={`status-badge ${executionResult.status === 'Accepted' ? 'accepted' : 'error'}`}>
                        <i className={`fas ${executionResult.status === 'Accepted' ? 'fa-check-circle' : 'fa-times-circle'}`}></i>
                        {executionResult.status}
                      </div>
                      {executionResult.runtime !== undefined && (
                        <div className="result-stat">
                          <span className="stat-label">Runtime:</span>
                          <span className="stat-value">{executionResult.runtime} ms</span>
                        </div>
                      )}
                      {executionResult.memory !== undefined && (
                        <div className="result-stat">
                          <span className="stat-label">Memory:</span>
                          <span className="stat-value">{(executionResult.memory / 1024).toFixed(1)} MB</span>
                        </div>
                      )}
                      {executionResult.output && (
                        <div className="result-output">
                          <strong>Output:</strong>
                          <pre>{executionResult.output}</pre>
                        </div>
                      )}
                      {executionResult.error && (
                        <div className="result-error">
                          <strong>Error:</strong>
                          <pre>{executionResult.error}</pre>
                        </div>
                      )}
                    </div>
                  )}
                  
                  {testResults.length > 0 && (
                    <div className="test-results-list">
                      <h4 style={{ marginBottom: '1rem', fontSize: '0.875rem', fontWeight: 600 }}>Test Cases:</h4>
                      {testResults.map((result, idx) => (
                        <div key={idx} className="test-result-item">
                          <div className="test-result-header">
                            <span className="test-case-name">Test Case {result.testCaseNumber}</span>
                            <span className={`test-status ${result.passed ? 'passed' : 'failed'}`}>
                              <i className={`fas ${result.passed ? 'fa-check-circle' : 'fa-times-circle'}`}></i>
                              {result.status}
                            </span>
                          </div>
                          {result.runtime !== undefined && (
                            <div className="test-result-detail">
                              <span>Runtime: {result.runtime} ms</span>
                              {result.memory !== undefined && <span>Memory: {(result.memory / 1024).toFixed(1)} MB</span>}
                            </div>
                          )}
                          {result.output && (
                            <div className="test-result-output">
                              <strong>Output:</strong> {result.output}
                            </div>
                          )}
                          {result.error && (
                            <div className="test-result-error">
                              <strong>Error:</strong> {result.error}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                  
                  {!executionResult && testResults.length === 0 && (
                    <div className="no-results">
                      <p>No test results yet. Click "Run" or "Submit" to execute your code.</p>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
