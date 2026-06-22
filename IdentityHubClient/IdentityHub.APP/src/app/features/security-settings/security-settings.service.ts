import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SecuritySettings {
  accessTokenMinutes: number;
  refreshTokenDays: number;
  maxLoginAttempts: number;
  lockDurationMinutes: number;
  requireEmailConfirmation: boolean;
}

export interface SecuritySettingsUpdateResponse {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class SecuritySettingsService {
  private readonly apiUrl = `${environment.apiUrl}/security-settings`;

  constructor(private readonly http: HttpClient) {}

  getSettings(): Observable<SecuritySettings> {
    return this.http.get<SecuritySettings>(this.apiUrl);
  }

  updateSettings(settings: SecuritySettings): Observable<SecuritySettingsUpdateResponse> {
    return this.http.put<SecuritySettingsUpdateResponse>(this.apiUrl, settings);
  }
}
