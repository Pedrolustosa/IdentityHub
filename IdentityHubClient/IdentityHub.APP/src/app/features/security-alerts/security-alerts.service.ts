import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface SecurityAlertFilters {
  type: string;
  userId: string;
  severity?: string;
  status?: string;
  fromDate: string;
  toDate: string;
}

interface SecurityAlertApiDto {
  id: string;
  userId: string;
  type: string;
  severity?: string;
  status?: string;
  description: string;
  createdAt: string;
}

interface PagedSecurityAlertsApiDto {
  items: SecurityAlertApiDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface SecurityAlertItem {
  id: string;
  userId: string;
  type: string;
  severity: string;
  status: string;
  description: string;
  createdAt: string;
}

export interface UpdateSecurityAlertStatusRequest {
  status: string;
}

export interface PagedSecurityAlerts {
  items: SecurityAlertItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class SecurityAlertsService {
  private readonly securityAlertsApiUrl = `${environment.apiUrl}/security-alerts`;

  constructor(private readonly http: HttpClient) {}

  getSecurityAlerts(page: number, pageSize: number, filters: SecurityAlertFilters): Observable<PagedSecurityAlerts> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    const requestParams = this.appendFilters(params, filters);

    return this.http.get<PagedSecurityAlertsApiDto>(this.securityAlertsApiUrl, { params: requestParams }).pipe(
      map((dto) => ({
        page: dto.page,
        pageSize: dto.pageSize,
        totalCount: dto.totalCount,
        totalPages: dto.totalPages,
        items: dto.items.map((item) => ({
          id: item.id,
          userId: item.userId,
          type: item.type,
          severity: item.severity ?? 'Low',
          status: item.status ?? 'Open',
          description: item.description,
          createdAt: item.createdAt
        }))
      }))
    );
  }

  getById(id: string): Observable<SecurityAlertItem> {
    return this.http.get<SecurityAlertItem>(`${this.securityAlertsApiUrl}/${encodeURIComponent(id)}`);
  }

  updateAlertStatus(id: string, body: UpdateSecurityAlertStatusRequest): Observable<string> {
    return this.http.put(`${this.securityAlertsApiUrl}/${encodeURIComponent(id)}/status`, body, {
      responseType: 'text'
    });
  }

  private appendFilters(params: HttpParams, filters: SecurityAlertFilters): HttpParams {
    let next = params;

    if (filters.type.trim()) {
      next = next.set('type', filters.type.trim());
    }

    if (filters.userId.trim()) {
      next = next.set('userId', filters.userId.trim());
    }

    if (filters.severity?.trim()) {
      next = next.set('severity', filters.severity.trim());
    }

    if (filters.status?.trim()) {
      next = next.set('status', filters.status.trim());
    }

    if (filters.fromDate.trim()) {
      next = next.set('fromDate', filters.fromDate.trim());
    }

    if (filters.toDate.trim()) {
      next = next.set('toDate', filters.toDate.trim());
    }

    return next;
  }
}
