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

  /** Stores access token based on remember-me and clears the opposite storage. */
  saveAccessToken(access: string, rememberMe: boolean): void {
    if (typeof window === 'undefined') {
      return;
    }
    const primary = rememberMe ? localStorage : sessionStorage;
    const secondary = rememberMe ? sessionStorage : localStorage;
    secondary.removeItem('accessToken');
    primary.setItem('accessToken', access);
  }

  /** Updates access token in whichever storage currently contains it. */
  updateAccessToken(access: string): void {
    if (typeof window === 'undefined') {
      return;
    }

    if (localStorage.getItem('accessToken') !== null) {
      localStorage.setItem('accessToken', access);
      return;
    }

    sessionStorage.setItem('accessToken', access);
  }

  clearAll(): void {
    if (typeof window === 'undefined') {
      return;
    }
    localStorage.removeItem('accessToken');
    sessionStorage.removeItem('accessToken');
  }
}
