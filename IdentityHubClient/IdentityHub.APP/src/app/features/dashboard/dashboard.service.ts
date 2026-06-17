import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

/** Shape returned by GET /api/Dashboard (camelCase serialization of `DashboardResponse`). */
interface DashboardApiDto {
  totalUsers: number;
  activeUsers: number;
  activeSessions: number;
  newUsers: number;
  securityEvents: number;
  openAlerts: number;
  loginsToday: number;
  auditedActionsToday: number;
  pendingInvites: number;
  totalRoles: number;
  suspiciousSessions: number;
  usersGrowth: number;
  sessionsGrowth: number;
  securityGrowth: number;
  systemStatus: DashboardSystemStatus;
  permissionCards: DashboardPermissionCard[];
  loginTrend: DashboardTrendPoint[];
  newUsersTrend: DashboardTrendPoint[];
  recentAuditActions: DashboardAuditAction[];
  recentSecurityAlerts: DashboardSecurityAlert[];
}

export interface DashboardSystemStatus {
  apiOnline: boolean;
  databaseConnected: boolean;
  emailConfigured: boolean;
}

export interface DashboardPermissionCard {
  permission: string;
  rolesGranted: number;
}

export interface DashboardTrendPoint {
  date: string;
  value: number;
}

export interface DashboardAuditAction {
  id: string;
  actorUserId: string;
  type: string;
  targetId: string | null;
  description: string;
  metadataJson: string | null;
  createdAt: string;
}

export interface DashboardSecurityAlert {
  id: string;
  userId: string;
  type: string;
  severity: string;
  status: string;
  description: string;
  createdAt: string;
}

export interface DashboardOverview {
  totalUsers: number;
  activeUsers: number;
  activeSessions: number;
  newUsersThisWeek: number;
  securityAlerts: number;
  openAlerts: number;
  loginsToday: number;
  auditedActionsToday: number;
  pendingInvites: number;
  totalRoles: number;
  suspiciousSessions: number;
  usersGrowthPercent: number;
  sessionsGrowthPercent: number;
  alertsGrowthPercent: number;
  systemStatus: DashboardSystemStatus;
  permissionCards: DashboardPermissionCard[];
  loginTrend: DashboardTrendPoint[];
  newUsersTrend: DashboardTrendPoint[];
  recentAuditActions: DashboardAuditAction[];
  recentSecurityAlerts: DashboardSecurityAlert[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly dashboardApiUrl = `${environment.apiUrl}/Dashboard`;

  constructor(private readonly http: HttpClient) {}

  getOverview(): Observable<DashboardOverview> {
    return this.http.get<DashboardApiDto>(this.dashboardApiUrl).pipe(
      map((dto) => ({
        totalUsers: dto.totalUsers,
        activeUsers: dto.activeUsers,
        activeSessions: dto.activeSessions,
        newUsersThisWeek: dto.newUsers,
        securityAlerts: dto.securityEvents,
        openAlerts: dto.openAlerts,
        loginsToday: dto.loginsToday,
        auditedActionsToday: dto.auditedActionsToday,
        pendingInvites: dto.pendingInvites,
        totalRoles: dto.totalRoles,
        suspiciousSessions: dto.suspiciousSessions,
        usersGrowthPercent: dto.usersGrowth,
        alertsGrowthPercent: dto.securityGrowth,
        sessionsGrowthPercent: dto.sessionsGrowth,
        systemStatus: dto.systemStatus,
        permissionCards: dto.permissionCards,
        loginTrend: dto.loginTrend,
        newUsersTrend: dto.newUsersTrend,
        recentAuditActions: dto.recentAuditActions,
        recentSecurityAlerts: dto.recentSecurityAlerts
      }))
    );
  }
}
