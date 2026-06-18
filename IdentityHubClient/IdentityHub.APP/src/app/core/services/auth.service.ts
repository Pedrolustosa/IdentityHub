import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { SessionTokensService } from './session-tokens.service';

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

export interface MeResponse {
  id: string;
  email: string | null;
  fullName: string | null;
  isActive: boolean;
  emailConfirmed: boolean;
  roles: string[];
  permissions: string[];
}

export interface UserSessionResponse {
  id: string;
  ipAddress: string;
  browser: string;
  operatingSystem: string;
  createdAt: string;
  lastAccessAt: string | null;
  revokedAt: string | null;
  isActive: boolean;
  isCurrent: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiBaseUrl = `${environment.apiUrl}/auth`;

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
    private readonly sessionTokens: SessionTokensService
  ) {}

  login(payload: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiBaseUrl}/login`, payload, { withCredentials: true });
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

  getMe(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.apiBaseUrl}/me`);
  }

  getSessions(): Observable<UserSessionResponse[]> {
    return this.http.get<UserSessionResponse[]>(`${this.apiBaseUrl}/sessions`);
  }

  getSessionsHistory(take = 20): Observable<UserSessionResponse[]> {
    const params = new HttpParams().set('take', take);
    return this.http.get<UserSessionResponse[]>(`${this.apiBaseUrl}/sessions/history`, { params });
  }

  getSecurityAlertCount(): Observable<number> {
    return this.http.get<number>(`${environment.apiUrl}/security-alerts/unread-count`);
  }

  revokeSession(sessionId: string): Observable<string> {
    return this.http.delete(`${this.apiBaseUrl}/sessions/${sessionId}`, {
      responseType: 'text'
    });
  }

  revokeOtherSessions(): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/sessions/others`);
  }

  revokeUserSessions(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/sessions/users/${encodeURIComponent(userId)}`);
  }

  getProfileSnapshotFromToken(): { email: string; fullName: string } | null {
    const token = this.sessionTokens.getAccessToken();
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
    return this.http
      .post<AuthResponse>(`${this.apiBaseUrl}/refresh`, {}, { withCredentials: true })
      .pipe(tap((res) => this.sessionTokens.updateAccessToken(res.token)));
  }

  isAuthenticated(): boolean {
    if (typeof window === 'undefined') {
      return false;
    }

    const token = this.sessionTokens.getAccessToken();
    if (!token) {
      return false;
    }

    return !this.isTokenExpired(token);
  }

  logout(): void {
    this.http
      .post(`${this.apiBaseUrl}/logout`, {}, { responseType: 'text', withCredentials: true })
      .subscribe({
        next: () => this.clearClientSessionAndNavigateToLogin(),
        error: () => this.clearClientSessionAndNavigateToLogin()
      });
  }

  clearClientSessionAndNavigateToLogin(): void {
    this.sessionTokens.clearAll();
    void this.router.navigate(['/login']);
  }

  /** Identity role names from the JWT (e.g. Admin, Manager, User). */
  getApplicationRoles(): string[] {
    if (typeof window === 'undefined') {
      return [];
    }
    const token = this.sessionTokens.getAccessToken();
    if (!token) {
      return [];
    }
    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return [];
    }
    const out = new Set<string>();
    for (const [key, value] of Object.entries(payload)) {
      if (key === 'role' || key.endsWith('/claims/role')) {
        if (Array.isArray(value)) {
          for (const v of value) {
            if (typeof v === 'string' && v.trim()) {
              out.add(v.trim());
            }
          }
        } else if (typeof value === 'string' && value.trim()) {
          out.add(value.trim());
        }
      }
    }
    return [...out];
  }

  hasApplicationRole(roleName: string): boolean {
    return this.getApplicationRoles().some((r) => r === roleName);
  }

  getApplicationPermissions(): string[] {
    if (typeof window === 'undefined') {
      return [];
    }

    const token = this.sessionTokens.getAccessToken();
    if (!token) {
      return [];
    }

    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return [];
    }

    const out = new Set<string>();

    for (const [key, value] of Object.entries(payload)) {
      if (key === 'permission') {
        if (Array.isArray(value)) {
          for (const entry of value) {
            if (typeof entry === 'string' && entry.trim()) {
              out.add(entry.trim());
            }
          }
        } else if (typeof value === 'string' && value.trim()) {
          out.add(value.trim());
        }
      }
    }

    return [...out];
  }

  hasPermission(permission: string): boolean {
    return this.getApplicationPermissions().some((entry) => entry === permission);
  }

  /** Role-permission assignment requires explicit permission. */
  canAssignRolePermissions(): boolean {
    return this.hasPermission('Roles.Permissions.Update');
  }

  canViewAuditLogs(): boolean {
    return this.hasPermission('Audit.View');
  }

  /** User id from JWT (`NameIdentifier` / `sub`). */
  getCurrentUserId(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }
    const token = this.sessionTokens.getAccessToken();
    if (!token) {
      return null;
    }
    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return null;
    }
    const nameIdKey = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';
    const raw = payload['sub'] ?? payload[nameIdKey];
    return typeof raw === 'string' && raw.length > 0 ? raw : null;
  }

  getCurrentSessionId(): string | null {
    if (typeof window === 'undefined') {
      return null;
    }

    const token = this.sessionTokens.getAccessToken();
    if (!token) {
      return null;
    }

    const payload = this.decodeTokenPayload(token);
    if (!payload) {
      return null;
    }

    const raw = payload['sid'];
    return typeof raw === 'string' && raw.length > 0 ? raw : null;
  }

  getCurrentUserDisplayName(): string {
    const token = this.sessionTokens.getAccessToken();
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
