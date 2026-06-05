import { Injectable } from '@angular/core';

/**
 * Single token storage abstraction (localStorage vs sessionStorage) so
 * interceptors and AuthService stay aligned without duplicating logic.
 */
@Injectable({ providedIn: 'root' })
export class SessionTokensService {
  getAccessToken(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }
    return localStorage.getItem('accessToken') ?? sessionStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }
    return localStorage.getItem('refreshToken') ?? sessionStorage.getItem('refreshToken');
  }

  /** Stores tokens based on remember-me and clears the opposite storage. */
  saveTokens(access: string, refresh: string, rememberMe: boolean): void {
    if (typeof window === 'undefined') {
      return;
    }
    const primary = rememberMe ? localStorage : sessionStorage;
    const secondary = rememberMe ? sessionStorage : localStorage;
    secondary.removeItem('accessToken');
    secondary.removeItem('refreshToken');
    primary.setItem('accessToken', access);
    primary.setItem('refreshToken', refresh);
  }

  /** Updates tokens in the same storage that already contains the refresh token. */
  updateTokens(access: string, refresh: string): void {
    if (typeof window === 'undefined') {
      return;
    }
    if (localStorage.getItem('refreshToken') !== null) {
      localStorage.setItem('accessToken', access);
      localStorage.setItem('refreshToken', refresh);
      return;
    }
    sessionStorage.setItem('accessToken', access);
    sessionStorage.setItem('refreshToken', refresh);
  }

  clearAll(): void {
    if (typeof window === 'undefined') {
      return;
    }
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    sessionStorage.removeItem('accessToken');
    sessionStorage.removeItem('refreshToken');
  }
}
