import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
    if (typeof window !== 'undefined') {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      sessionStorage.removeItem('accessToken');
      sessionStorage.removeItem('refreshToken');
    }
    void this.router.navigate(['/login']);
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
