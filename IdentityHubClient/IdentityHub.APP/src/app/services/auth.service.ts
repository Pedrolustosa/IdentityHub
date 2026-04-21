import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Router } from '@angular/router';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UpdateProfileRequest {
  fullName: string;
  email: string;
}

export interface ProfileResponse {
  id: string;
  email: string | null;
  fullName: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBaseUrl = 'https://localhost:7039/api/auth';

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiBaseUrl}/login`, payload);
  }

  register(payload: RegisterRequest): Observable<string> {
    return this.http.post(`${this.apiBaseUrl}/register`, payload, {
      responseType: 'text'
    });
  }

  confirmEmail(email: string, token: string): Observable<string> {
    const params = new HttpParams().set('email', email).set('token', token);
    return this.http.get(`${this.apiBaseUrl}/confirm-email`, {
      params,
      responseType: 'text'
    });
  }

  resendConfirmation(payload: ForgotPasswordRequest): Observable<string> {
    return this.http.post(`${this.apiBaseUrl}/resend-confirmation`, payload, {
      responseType: 'text'
    });
  }

  forgotPassword(payload: ForgotPasswordRequest): Observable<string> {
    return this.http.post(`${this.apiBaseUrl}/forgot-password`, payload, {
      responseType: 'text'
    });
  }

  resetPassword(payload: ResetPasswordRequest): Observable<string> {
    return this.http.post(`${this.apiBaseUrl}/reset-password`, payload, {
      responseType: 'text'
    });
  }

  changePassword(payload: ChangePasswordRequest): Observable<string> {
    return this.http.post(`${this.apiBaseUrl}/change-password`, payload, {
      responseType: 'text'
    });
  }

  updateProfile(payload: UpdateProfileRequest): Observable<ProfileResponse> {
    return this.http.put<ProfileResponse>(`${this.apiBaseUrl}/profile`, payload);
  }

  getProfileSnapshotFromToken(): { email: string; fullName: string } | null {
    const token = localStorage.getItem('accessToken') ?? sessionStorage.getItem('accessToken');
    if (!token) {
      return null;
    }
    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return null;
    }

    const email = (
      payload['email'] ??
      payload['unique_name'] ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
      ''
    ).toString();

    const fullName = (payload['fullName'] ?? payload['full_name'] ?? '').toString();

    return { email, fullName };
  }

  refreshSession(): Observable<AuthResponse> {
    const refreshToken = this.getStoredRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token'));
    }

    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/refresh`, { refreshToken })
      .pipe(tap((res) => this.persistTokens(res)));
  }

  isAuthenticated(): boolean {
    if (typeof window === 'undefined') {
      return false;
    }

    const token = localStorage.getItem('accessToken') ?? sessionStorage.getItem('accessToken');
    if (!token) {
      return false;
    }

    return !this.isTokenExpired(token);
  }

  logout(): void {
    const refreshToken = this.getStoredRefreshToken();

    if (!refreshToken) {
      this.clearClientSessionAndNavigateToLogin();
      return;
    }

    this.http
      .post(`${this.apiBaseUrl}/logout`, { refreshToken }, { responseType: 'text' })
      .subscribe({
        next: () => this.clearClientSessionAndNavigateToLogin(),
        error: () => this.clearClientSessionAndNavigateToLogin()
      });
  }

  clearClientSessionAndNavigateToLogin(): void {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      sessionStorage.removeItem('accessToken');
      sessionStorage.removeItem('refreshToken');
    }
    void this.router.navigate(['/login']);
  }

  private getStoredRefreshToken(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }
    return localStorage.getItem('refreshToken') ?? sessionStorage.getItem('refreshToken');
  }

  private persistTokens(res: AuthResponse): void {
    if (typeof window === 'undefined') {
      return;
    }
    const storage =
      localStorage.getItem('refreshToken') !== null ? localStorage : sessionStorage;
    storage.setItem('accessToken', res.token);
    storage.setItem('refreshToken', res.refreshToken);
  }

  getCurrentUserDisplayName(): string {
    const token = localStorage.getItem('accessToken') ?? sessionStorage.getItem('accessToken');
    if (!token) {
      return 'User';
    }

    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return 'User';
    }

    const fullName =
      payload['fullName'] ??
      payload['full_name'] ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'];
    const name =
      payload['name'] ??
      payload['unique_name'] ??
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'];

    return (fullName || name || 'User').toString();
  }

  private decodeTokenPayload(token: string): Record<string, unknown> | null {
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) {
        return null;
      }

      const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const payloadJson = atob(base64);
      return JSON.parse(payloadJson) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = this.decodeTokenPayload(token) as { exp?: number } | null;
      if (!payload) {
        return true;
      }

      if (!payload.exp) {
        return true;
      }

      const currentUnixTime = Math.floor(Date.now() / 1000);
      return payload.exp <= currentUnixTime;
    } catch {
      return true;
    }
  }
}
