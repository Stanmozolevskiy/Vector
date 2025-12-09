import { expect, afterEach, beforeAll, afterAll, vi } from 'vitest';
import { cleanup } from '@testing-library/react';
import * as matchers from '@testing-library/jest-dom/matchers';
import { server } from './mocks/server';

// Extend Vitest's expect with jest-dom matchers
expect.extend(matchers);

// Mock localStorage for jsdom environment
if (typeof window !== 'undefined' && !window.localStorage) {
  const localStorageMock = (() => {
    let store: Record<string, string> = {};
    return {
      getItem: (key: string) => store[key] || null,
      setItem: (key: string, value: string) => {
        store[key] = value.toString();
      },
      removeItem: (key: string) => {
        delete store[key];
      },
      clear: () => {
        store = {};
      },
    };
  })();

  Object.defineProperty(window, 'localStorage', {
    value: localStorageMock,
    writable: true,
  });
}

// Suppress console errors for CSS parsing issues in jsdom
// This is a known issue with jsdom and CSS variables in border shorthand
const originalError = console.error;
const originalWarn = console.warn;
console.error = (...args: unknown[]) => {
  // Filter out CSS parsing errors from jsdom/cssstyle
  const errorMsg = args[0]?.toString() || '';
  if (errorMsg.includes('border-width') || errorMsg.includes('Cannot create property')) {
    return;
  }
  originalError(...args);
};
console.warn = (...args: unknown[]) => {
  // Filter out CSS parsing warnings
  const warnMsg = args[0]?.toString() || '';
  if (warnMsg.includes('border-width') || warnMsg.includes('CSS')) {
    return;
  }
  originalWarn(...args);
};

// Start MSW server before all tests
beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));

// Reset handlers after each test
afterEach(() => {
  cleanup();
  server.resetHandlers();
});

// Clean up after all tests
afterAll(() => server.close());

