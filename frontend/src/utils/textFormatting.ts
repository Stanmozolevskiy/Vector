/**
 * Normalizes text by trimming leading/trailing spaces and
 * reducing multiple internal spaces to a single space.
 */
export const normalizeSpaces = (text: string | undefined | null): string => {
  if (!text) return '';
  return text.trim().replace(/\s+/g, ' ');
};

/**
 * Normalizes spaces and capitalizes the first letter of each word.
 * In every word, only the first letter can be capital, and the rest is lowercase.
 */
export const normalizeSentence = (text: string | undefined | null): string => {
  const normalized = normalizeSpaces(text);
  if (!normalized) return '';
  return normalized
    .split(' ')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ');
};

/**
 * Normalizes spaces and capitalizes the first letter of each word.
 * Suitable for names.
 */
export const normalizeName = (name: string | undefined | null): string => {
  return normalizeSentence(name);
};
