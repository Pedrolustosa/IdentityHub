import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AuditLogFilters {
  type: string;
  actorUserId: string;
  description: string;
  fromDate: string;
  toDate: string;
}

interface AuditLogApiDto {
  id: string;
  actorUserId: string;
  type: string;
  description: string;
  createdAt: string;
}

interface PagedAuditLogApiDto {
  items: AuditLogApiDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuditLogItem {
  id: string;
  actorUserId: string;
  type: string;
  description: string;
  createdAt: string;
}

export interface PagedAuditLogs {
  items: AuditLogItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class AuditLogsService {
  private readonly auditLogsApiUrl = `${environment.apiUrl}/audit-logs`;

  constructor(private readonly http: HttpClient) {}

  getAuditLogs(page: number, pageSize: number, filters: AuditLogFilters): Observable<PagedAuditLogs> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    const requestParams = this.appendFilters(params, filters);

    return this.http.get<PagedAuditLogApiDto>(this.auditLogsApiUrl, { params: requestParams });
  }

  exportAuditLogsCsv(filters: AuditLogFilters): Observable<Blob> {
    const params = this.appendFilters(new HttpParams(), filters);

    return this.http.get(`${this.auditLogsApiUrl}/export`, {
      params,
      responseType: 'blob'
    });
  }

  private appendFilters(params: HttpParams, filters: AuditLogFilters): HttpParams {
    let next = params;

    if (filters.type.trim()) {
      next = next.set('type', filters.type.trim());
    }

    if (filters.actorUserId.trim()) {
      next = next.set('actorUserId', filters.actorUserId.trim());
    }

    if (filters.description.trim()) {
      next = next.set('description', filters.description.trim());
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
