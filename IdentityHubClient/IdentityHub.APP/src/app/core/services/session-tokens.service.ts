import { Injectable } from '@angular/core';

const REMEMBER_PREFERENCE_KEY = 'ih_remember_me';
const LEGACY_ACCESS_TOKEN_KEY = 'accessToken';

/**
 * Access tokens stay in memory only (not local/sessionStorage) to reduce XSS theft surface.
 * Session continuity across reloads uses the HttpOnly refresh cookie + silent refresh bootstrap.
 */
@Injectable({ providedIn: 'root' })
export class SessionTokensService {
  private accessToken: string | null = null;

  getAccessToken(): string | null {
    return this.accessToken;
  }

  /** Stores access token in memory and records the remember-me preference (not the JWT). */
  saveAccessToken(access: string, rememberMe: boolean): void {
    this.accessToken = access;
    this.persistRememberPreference(rememberMe);
    this.clearLegacyStorage();
  }

  /** Updates the in-memory access token after refresh. */
  updateAccessToken(access: string): void {
    this.accessToken = access;
    this.clearLegacyStorage();
  }

  /** Alias used by bootstrap / refresh paths. */
  setAccessTokenInMemory(access: string): void {
    this.updateAccessToken(access);
  }

  getRememberMePreference(): boolean {
    if (typeof window === 'undefined') {
      return true;
    }

    const raw = localStorage.getItem(REMEMBER_PREFERENCE_KEY) ?? sessionStorage.getItem(REMEMBER_PREFERENCE_KEY);
    if (raw === null) {
      return true;
    }

    return raw === '1';
  }

  clearAll(): void {
    this.accessToken = null;
    if (typeof window === 'undefined') {
      return;
    }

    localStorage.removeItem(REMEMBER_PREFERENCE_KEY);
    sessionStorage.removeItem(REMEMBER_PREFERENCE_KEY);
    this.clearLegacyStorage();
  }

  /** Removes JWTs previously persisted in web storage by older builds. */
  clearLegacyAccessTokens(): void {
    this.clearLegacyStorage();
  }

  private persistRememberPreference(rememberMe: boolean): void {
    if (typeof window === 'undefined') {
      return;
    }

    const primary = rememberMe ? localStorage : sessionStorage;
    const secondary = rememberMe ? sessionStorage : localStorage;
    secondary.removeItem(REMEMBER_PREFERENCE_KEY);
    primary.setItem(REMEMBER_PREFERENCE_KEY, rememberMe ? '1' : '0');
  }

  private clearLegacyStorage(): void {
    if (typeof window === 'undefined') {
      return;
    }

    localStorage.removeItem(LEGACY_ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(LEGACY_ACCESS_TOKEN_KEY);
  }
}
