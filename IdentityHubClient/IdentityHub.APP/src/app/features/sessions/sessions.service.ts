import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminSessionItem {
  id: string;
  userId: string;
  email?: string | null;
  fullName?: string | null;
  ipAddress: string;
  browser: string;
  operatingSystem: string;
  createdAt: string;
  lastAccessAt?: string | null;
  revokedAt?: string | null;
  isActive: boolean;
  isCurrent: boolean;
}

export interface PagedAdminSessions {
  items: AdminSessionItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminSessionFilters {
  activeOnly: boolean;
  search: string;
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class SessionsService {
  private readonly apiBaseUrl = `${environment.apiUrl}/sessions`;

  constructor(private readonly http: HttpClient) {}

  getSessions(page = 1, pageSize = 20, filters: AdminSessionFilters): Observable<PagedAdminSessions> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('activeOnly', filters.activeOnly);

    if (filters.search.trim()) {
      params = params.set('search', filters.search.trim());
    }

    if (filters.userId.trim()) {
      params = params.set('userId', filters.userId.trim());
    }

    return this.http.get<PagedAdminSessions>(this.apiBaseUrl, { params });
  }

  revokeSession(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/${encodeURIComponent(sessionId)}`);
  }
}
