import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
    return this.http.get<DashboardSummary>(this.dashboardApiUrl);
  }
}
