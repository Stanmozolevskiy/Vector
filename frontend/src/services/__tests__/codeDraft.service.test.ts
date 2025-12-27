import { describe, it, expect, vi, beforeEach } from 'vitest';
import api from '../api';
import { codeDraftService } from '../codeDraft.service';

// Mock the api module
vi.mock('../api', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('codeDraftService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getCodeDraft', () => {
    it('should return code draft when it exists', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';
      const mockDraft = {
        id: 'draft-id',
        questionId,
        language,
        code: 'function test() { return true; }',
        updatedAt: new Date().toISOString(),
      };

      (api.get as any).mockResolvedValue({
        status: 200,
        data: mockDraft,
      });

      const result = await codeDraftService.getCodeDraft(questionId, language);

      expect(result).toEqual(mockDraft);
      expect(api.get).toHaveBeenCalledWith(
        `/code-drafts/${questionId}/${language}`,
        { validateStatus: expect.any(Function) }
      );
    });

    it('should return null when code draft does not exist (404)', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';

      (api.get as any).mockResolvedValue({
        status: 404,
        data: null,
      });

      const result = await codeDraftService.getCodeDraft(questionId, language);

      expect(result).toBeNull();
      expect(api.get).toHaveBeenCalledWith(
        `/code-drafts/${questionId}/${language}`,
        { validateStatus: expect.any(Function) }
      );
    });

    it('should not throw error on 404 - returns null silently', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';

      (api.get as any).mockResolvedValue({
        status: 404,
        data: null,
      });

      // Should not throw, should return null
      const result = await codeDraftService.getCodeDraft(questionId, language);
      expect(result).toBeNull();
    });

    it('should throw error for non-404 errors', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';
      const error = new Error('Server error');
      (error as any).response = { status: 500 };

      (api.get as any).mockRejectedValue(error);

      await expect(
        codeDraftService.getCodeDraft(questionId, language)
      ).rejects.toThrow('Server error');
    });

    it('should use validateStatus to prevent axios from logging 404', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';

      (api.get as any).mockResolvedValue({
        status: 404,
        data: null,
      });

      await codeDraftService.getCodeDraft(questionId, language);

      const callArgs = (api.get as any).mock.calls[0];
      const validateStatus = callArgs[1].validateStatus;
      
      // validateStatus should return true for 200 and 404
      expect(validateStatus(200)).toBe(true);
      expect(validateStatus(404)).toBe(true);
      expect(validateStatus(500)).toBe(false);
    });
  });

  describe('saveCodeDraft', () => {
    it('should save code draft successfully', async () => {
      const request = {
        questionId: 'test-question-id',
        language: 'javascript',
        code: 'function test() { return true; }',
      };

      const mockSavedDraft = {
        id: 'draft-id',
        ...request,
        updatedAt: new Date().toISOString(),
      };

      (api.post as any).mockResolvedValue({
        data: mockSavedDraft,
      });

      const result = await codeDraftService.saveCodeDraft(request);

      expect(result).toEqual(mockSavedDraft);
      expect(api.post).toHaveBeenCalledWith('/code-drafts', request);
    });
  });

  describe('deleteCodeDraft', () => {
    it('should delete code draft successfully', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';

      (api.delete as any).mockResolvedValue({});

      await codeDraftService.deleteCodeDraft(questionId, language);

      expect(api.delete).toHaveBeenCalledWith(
        `/code-drafts/${questionId}/${language}`
      );
    });
  });
});

