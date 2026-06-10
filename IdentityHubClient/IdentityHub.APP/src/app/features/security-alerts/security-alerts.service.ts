import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface SecurityAlertFilters {
  type: string;
  userId: string;
  fromDate: string;
  toDate: string;
}

interface SecurityAlertApiDto {
  id: string;
  userId: string;
  type: string;
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
  description: string;
  createdAt: string;
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

    return this.http.get<PagedSecurityAlertsApiDto>(this.securityAlertsApiUrl, { params: requestParams });
  }

  private appendFilters(params: HttpParams, filters: SecurityAlertFilters): HttpParams {
    let next = params;

    if (filters.type.trim()) {
      next = next.set('type', filters.type.trim());
    }

    if (filters.userId.trim()) {
      next = next.set('userId', filters.userId.trim());
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