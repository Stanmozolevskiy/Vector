/**
 * Code Auto-Save Utility
 * Handles automatic saving and restoration of user code per question and language
 */

const STORAGE_PREFIX = 'vector_code_';
const STORAGE_KEY_SEPARATOR = '_';

/**
 * Generate storage key for a question and language combination
 */
const getStorageKey = (questionId: string, language: string): string => {
  return `${STORAGE_PREFIX}${questionId}${STORAGE_KEY_SEPARATOR}${language}`;
};

/**
 * Save code to localStorage
 * @param questionId - The question ID
 * @param language - The programming language
 * @param code - The code to save
 * @returns true if successful, false otherwise
 */
export const saveCode = (questionId: string, language: string, code: string): boolean => {
  try {
    const key = getStorageKey(questionId, language);
    localStorage.setItem(key, code);
    return true;
  } catch (error) {
    // Handle quota exceeded error
    if (error instanceof DOMException && error.name === 'QuotaExceededError') {
      console.warn('localStorage quota exceeded. Attempting to clear old entries...');
      try {
        // Clear old entries (keep only last 50 entries)
        clearOldEntries(50);
        // Retry saving
        const key = getStorageKey(questionId, language);
        localStorage.setItem(key, code);
        return true;
      } catch (retryError) {
        console.error('Failed to save code after clearing old entries:', retryError);
        return false;
      }
    }
    console.error('Failed to save code:', error);
    return false;
  }
};

/**
 * Load code from localStorage
 * @param questionId - The question ID
 * @param language - The programming language
 * @returns The saved code or null if not found
 */
export const loadCode = (questionId: string, language: string): string | null => {
  try {
    const key = getStorageKey(questionId, language);
    return localStorage.getItem(key);
  } catch (error) {
    console.error('Failed to load code:', error);
    return null;
  }
};

/**
 * Clear saved code for a specific question and language
 * @param questionId - The question ID
 * @param language - The programming language
 */
export const clearCode = (questionId: string, language: string): void => {
  try {
    const key = getStorageKey(questionId, language);
    localStorage.removeItem(key);
  } catch (error) {
    console.error('Failed to clear code:', error);
  }
};

/**
 * Clear all saved code for a question (all languages)
 * @param questionId - The question ID
 */
export const clearAllCodeForQuestion = (questionId: string): void => {
  try {
    const keys = Object.keys(localStorage);
    keys.forEach(key => {
      if (key.startsWith(`${STORAGE_PREFIX}${questionId}${STORAGE_KEY_SEPARATOR}`)) {
        localStorage.removeItem(key);
      }
    });
  } catch (error) {
    console.error('Failed to clear all code for question:', error);
  }
};

/**
 * Clear old entries to free up space
 * @param keepCount - Number of recent entries to keep
 */
const clearOldEntries = (keepCount: number): void => {
  try {
    const keys = Object.keys(localStorage)
      .filter(key => key.startsWith(STORAGE_PREFIX))
      .map(key => ({
        key,
        timestamp: localStorage.getItem(`${key}_timestamp`) || '0'
      }))
      .sort((a, b) => parseInt(b.timestamp) - parseInt(a.timestamp));

    // Remove oldest entries
    const toRemove = keys.slice(keepCount);
    toRemove.forEach(({ key }) => {
      localStorage.removeItem(key);
      localStorage.removeItem(`${key}_timestamp`);
    });
  } catch (error) {
    console.error('Failed to clear old entries:', error);
  }
};

/**
 * Check if code has unsaved changes
 * @param questionId - The question ID
 * @param language - The programming language
 * @param currentCode - The current code in the editor
 * @returns true if there are unsaved changes
 */
export const hasUnsavedChanges = (questionId: string, language: string, currentCode: string): boolean => {
  const savedCode = loadCode(questionId, language);
  return savedCode !== null && savedCode !== currentCode;
};

/**
 * Get all saved question IDs
 * @returns Array of question IDs that have saved code
 */
export const getSavedQuestionIds = (): string[] => {
  try {
    const keys = Object.keys(localStorage)
      .filter(key => key.startsWith(STORAGE_PREFIX))
      .map(key => {
        // Extract question ID from key format: vector_code_{questionId}_{language}
        const parts = key.replace(STORAGE_PREFIX, '').split(STORAGE_KEY_SEPARATOR);
        return parts[0]; // Return question ID
      });
    
    // Return unique question IDs
    return Array.from(new Set(keys));
  } catch (error) {
    console.error('Failed to get saved question IDs:', error);
    return [];
  }
};

