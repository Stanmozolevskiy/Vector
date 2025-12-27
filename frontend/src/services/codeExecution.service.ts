import api from './api';

export interface ExecutionRequest {
  sourceCode: string;
  language: string;
  stdin?: string;
}

// Backend DTO mapping
export interface ExecutionRequestDto {
  sourceCode: string;
  language: string;
  stdin?: string;
}

export interface ExecutionResult {
  output: string;
  error?: string;
  status: string;
  runtime: number;
  memory: number;
  compileOutput?: string;
}

export interface TestResult {
  testCaseId: string;
  testCaseNumber: number;
  passed: boolean;
  stdout?: string; // Debug output (console.log/print)
  output?: string; // Actual return value
  expectedOutput?: string; // Added for LeetCode-style comparison
  error?: string;
  runtime: number;
  memory: number;
  status: string;
  input?: string; // Added to show input in results
}

export interface RunCodeWithTestCasesRequest {
  sourceCode: string;
  language: string;
  testCaseText: string;
}

export interface CaseResult {
  caseIndex: number;
  inputValues: any[];
  parameterNames: string[];
  stdout: string;
  output?: string;
  expectedOutput?: string;
  passed?: boolean;
  runtime: number;
  memory: number;
  status: string;
  error?: {
    message: string;
    stack?: string;
  };
}

export interface TestCaseParseError {
  type: string;
  message: string;
  lineNumber?: number;
}

export interface RunResult {
  status: string; // ACCEPTED, WRONG_ANSWER, RUNTIME_ERROR, TLE, INVALID_INPUT
  runtimeMs?: number;
  memoryMb?: number;
  cases: CaseResult[];
  validationError?: TestCaseParseError;
}

export interface SupportedLanguage {
  name: string;
  value: string;
  judge0LanguageId: number;
  version: string;
}

export const codeExecutionService = {
  async executeCode(request: ExecutionRequest): Promise<ExecutionResult> {
    // Map frontend property names to backend DTO property names
    const backendRequest = {
      sourceCode: request.sourceCode,
      language: request.language,
      stdin: request.stdin,
    };
    const response = await api.post<ExecutionResult>('/CodeExecution/execute', backendRequest);
    return response.data;
  },

  async runCode(questionId: string, request: ExecutionRequest): Promise<TestResult[]> {
    // Run against visible (non-hidden) test cases
    const backendRequest = {
      sourceCode: request.sourceCode,
      language: request.language,
      stdin: request.stdin,
    };
    const response = await api.post<TestResult[]>(`/CodeExecution/run/${questionId}`, backendRequest);
    return response.data;
  },

  async validateSolution(questionId: string, request: ExecutionRequest): Promise<TestResult[]> {
    // Validate against all test cases (including hidden) for Submit
    const backendRequest = {
      sourceCode: request.sourceCode,
      language: request.language,
      stdin: request.stdin,
    };
    const response = await api.post<TestResult[]>(`/CodeExecution/validate/${questionId}`, backendRequest);
    return response.data;
  },

  async getSupportedLanguages(): Promise<SupportedLanguage[]> {
    const response = await api.get<SupportedLanguage[]>('/CodeExecution/languages');
    return response.data;
  },

  async runCodeWithTestCases(questionId: string, request: RunCodeWithTestCasesRequest): Promise<RunResult> {
    const response = await api.post<RunResult>(`/CodeExecution/run-with-testcases/${questionId}`, request);
    return response.data;
  },
};

