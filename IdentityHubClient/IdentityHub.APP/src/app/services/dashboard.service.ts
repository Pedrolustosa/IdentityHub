import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

/** Shape returned by GET /api/Dashboard (ASP.NET camelCase serialization of DashboardResponse). */
interface DashboardApiDto {
  totalUsers: number;
  activeSessions: number;
  newUsers: number;
  newUsersGrowth: number;
  securityAlerts: number;
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
  private readonly dashboardApiUrl = 'https://localhost:7039/api/Dashboard';

  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardApiDto>(this.dashboardApiUrl).pipe(
      map((dto) => ({
        totalUsers: dto.totalUsers,
        activeSessions: dto.activeSessions,
        newUsersThisWeek: dto.newUsers,
        securityAlerts: dto.securityAlerts,
        usersGrowthPercent: dto.newUsersGrowth,
        alertsGrowthPercent: dto.securityGrowth,
        sessionsGrowthPercent: 0
      }))
    );
  }
}
