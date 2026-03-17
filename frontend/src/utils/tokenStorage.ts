/**
 * Token storage that respects "Remember Me":
 * - Remember Me checked → localStorage (persists across browser restarts)
 * - Remember Me unchecked → sessionStorage (cleared when tab/browser closes)
 * BUG-Login-03: session persistence must depend on the checkbox.
 */

const ACCESS_KEY = 'accessToken';
const REFRESH_KEY = 'refreshToken';
const TOKEN_TYPE_KEY = 'tokenType';

type Store = typeof sessionStorage | typeof localStorage;

function getStoreWithTokens(): Store | null {
  if (typeof sessionStorage === 'undefined' || typeof localStorage === 'undefined') return null;
  if (sessionStorage.getItem(REFRESH_KEY)) return sessionStorage;
  if (localStorage.getItem(REFRESH_KEY)) return localStorage;
  return null;
}

export const tokenStorage = {
  getAccessToken(): string | null {
    const store = getStoreWithTokens();
    if (store) return store.getItem(ACCESS_KEY);
    return sessionStorage?.getItem(ACCESS_KEY) ?? localStorage?.getItem(ACCESS_KEY) ?? null;
  },

  getRefreshToken(): string | null {
    const store = getStoreWithTokens();
    if (store) return store.getItem(REFRESH_KEY);
    return sessionStorage?.getItem(REFRESH_KEY) ?? localStorage?.getItem(REFRESH_KEY) ?? null;
  },

  getTokenType(): string | null {
    const store = getStoreWithTokens();
    if (store) return store.getItem(TOKEN_TYPE_KEY);
    return sessionStorage?.getItem(TOKEN_TYPE_KEY) ?? localStorage?.getItem(TOKEN_TYPE_KEY) ?? null;
  },

  /**
   * Store tokens after login. Use sessionStorage when remember=false so the session
   * ends when the browser is closed; use localStorage when remember=true.
   */
  setTokens(accessToken: string, refreshToken: string, tokenType: string, remember: boolean): void {
    const persistent = remember ? localStorage : sessionStorage;
    const session = remember ? sessionStorage : localStorage;
    persistent.setItem(ACCESS_KEY, accessToken);
    persistent.setItem(REFRESH_KEY, refreshToken);
    persistent.setItem(TOKEN_TYPE_KEY, tokenType);
    session.removeItem(ACCESS_KEY);
    session.removeItem(REFRESH_KEY);
    session.removeItem(TOKEN_TYPE_KEY);
  },

  /**
   * Store new tokens after refresh. Uses the same store that currently holds the refresh token.
   */
  setTokensFromRefresh(accessToken: string, refreshToken: string): void {
    const store = getStoreWithTokens();
    if (!store) return;
    store.setItem(ACCESS_KEY, accessToken);
    store.setItem(REFRESH_KEY, refreshToken);
  },

  clearTokens(): void {
    sessionStorage?.removeItem(ACCESS_KEY);
    sessionStorage?.removeItem(REFRESH_KEY);
    sessionStorage?.removeItem(TOKEN_TYPE_KEY);
    localStorage?.removeItem(ACCESS_KEY);
    localStorage?.removeItem(REFRESH_KEY);
    localStorage?.removeItem(TOKEN_TYPE_KEY);
  },

  hasTokens(): boolean {
    return !!(this.getRefreshToken() || this.getAccessToken());
  },
};
