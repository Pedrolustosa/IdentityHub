import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

/** Shape returned by GET /api/Dashboard (camelCase serialization of `DashboardResponse`). */
interface DashboardApiDto {
  totalUsers: number;
  activeSessions: number;
  newUsers: number;
  securityEvents: number;
  usersGrowth: number;
  sessionsGrowth: number;
  securityGrowth: number;
}

export interface DashboardSummary {
  totalUsers: number;
  activeSessions: number;
  newUsersThisWeek: number;
  securityAlerts: number;
  usersGrowthPercent: number;
  sessionsGrowthPercent: number;
  alertsGrowthPercent: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly dashboardApiUrl = `${environment.apiUrl}/Dashboard`;

  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardApiDto>(this.dashboardApiUrl).pipe(
      map((dto) => ({
        totalUsers: dto.totalUsers,
        activeSessions: dto.activeSessions,
        newUsersThisWeek: dto.newUsers,
        securityAlerts: dto.securityEvents,
        usersGrowthPercent: dto.usersGrowth,
        alertsGrowthPercent: dto.securityGrowth,
        sessionsGrowthPercent: dto.sessionsGrowth
      }))
    );
  }
}
