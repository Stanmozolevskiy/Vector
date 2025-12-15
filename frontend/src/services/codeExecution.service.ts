import api from './api';

export interface ExecutionRequest {
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
  output?: string;
  error?: string;
  runtime: number;
  memory: number;
  status: string;
}

export interface SupportedLanguage {
  name: string;
  value: string;
  judge0LanguageId: number;
  version: string;
}

export const codeExecutionService = {
  async executeCode(request: ExecutionRequest): Promise<ExecutionResult> {
    const response = await api.post<ExecutionResult>('/codeexecution/execute', request);
    return response.data;
  },

  async validateSolution(questionId: string, request: ExecutionRequest): Promise<TestResult[]> {
    const response = await api.post<TestResult[]>(`/codeexecution/validate/${questionId}`, request);
    return response.data;
  },

  async getSupportedLanguages(): Promise<SupportedLanguage[]> {
    const response = await api.get<SupportedLanguage[]>('/codeexecution/languages');
    return response.data;
  },
};

