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
        data: mockDraft,
      });

      const result = await codeDraftService.getCodeDraft(questionId, language);

      expect(result).toEqual(mockDraft);
      expect(api.get).toHaveBeenCalledWith(
        `/code-drafts/${questionId}/${language}`
      );
    });

    it('should return null when code draft does not exist (404)', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';
      const error = new Error('Not found');
      (error as any).response = { status: 404 };

      (api.get as any).mockRejectedValue(error);

      const result = await codeDraftService.getCodeDraft(questionId, language);

      expect(result).toBeNull();
      expect(api.get).toHaveBeenCalledWith(
        `/code-drafts/${questionId}/${language}`
      );
    });

    it('should not throw error on 404 - returns null silently', async () => {
      const questionId = 'test-question-id';
      const language = 'javascript';
      const error = new Error('Not found');
      (error as any).response = { status: 404 };

      (api.get as any).mockRejectedValue(error);

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

