import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import {
  DashboardAuditAction,
  DashboardOverview,
  DashboardPermissionCard,
  DashboardService,
  DashboardTrendPoint
} from '../../dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, UxStateComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  overview: DashboardOverview | null = null;
  selectedTrendWindow: 7 | 30 = 30;
  readonly canViewSecurityAlerts: boolean;
  readonly canViewAudit: boolean;
  readonly canCreateUsers: boolean;
  readonly canManagePermissions: boolean;

  readonly trendWindowOptions: Array<7 | 30> = [7, 30];

  constructor(
    private readonly dashboardService: DashboardService,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {
    this.canViewSecurityAlerts = this.authService.hasPermission('SecurityEvents.View');
    this.canViewAudit = this.authService.hasPermission('Audit.View');
    this.canCreateUsers = this.authService.hasPermission('Users.Create');
    this.canManagePermissions = this.authService.hasPermission('Roles.Permissions.Update');
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.loadError = null;

    this.dashboardService
      .getOverview()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (data) => {
          this.overview = data;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.overview = null;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Dashboard');
        }
      });
  }

  setTrendWindow(days: 7 | 30): void {
    this.selectedTrendWindow = days;
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(value);
  }

  formatCompactDate(value: string): string {
    const date = new Date(value);
    return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(date);
  }

  formatFullDateTime(value: string): string {
    const date = new Date(value);
    return new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }

  formatGrowthPercent(value: number): string {
    const rounded = Math.round(value * 100) / 100;
    if (rounded === 0) {
      return '0%';
    }
    const sign = rounded > 0 ? '+' : '';
    return `${sign}${rounded}%`;
  }

  growthTone(value: number): 'positive' | 'negative' | 'neutral' {
    if (value > 0) {
      return 'positive';
    }
    if (value < 0) {
      return 'negative';
    }
    return 'neutral';
  }

  signupTrendBadgeClass(value: number): string {
    switch (this.growthTone(value)) {
      case 'positive':
        return 'bg-success-50 text-success-800 ring-1 ring-success-200/80 dark:bg-success-900/40 dark:text-success-300 dark:ring-success-800/80';
      case 'negative':
        return 'bg-danger-50 text-danger-800 ring-1 ring-danger-200/80 dark:bg-danger-900/40 dark:text-danger-300 dark:ring-danger-800/80';
      default:
        return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
    }
  }

  alertsTrendBadgeClass(value: number): string {
    switch (this.growthTone(value)) {
      case 'positive':
        return 'bg-danger-50 text-danger-800 ring-1 ring-danger-200/80 dark:bg-danger-900/40 dark:text-danger-300 dark:ring-danger-800/80';
      case 'negative':
        return 'bg-success-50 text-success-800 ring-1 ring-success-200/80 dark:bg-success-900/40 dark:text-success-300 dark:ring-success-800/80';
      default:
        return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
    }
  }

  signupTrendShortLabel(value: number): string {
    const t = this.growthTone(value);
    if (t === 'positive') {
      return 'Signups up';
    }
    if (t === 'negative') {
      return 'Signups down';
    }
    return 'Signups flat';
  }

  alertsTrendShortLabel(value: number): string {
    const t = this.growthTone(value);
    if (t === 'positive') {
      return 'Alerts rising';
    }
    if (t === 'negative') {
      return 'Alerts falling';
    }
    return 'Alerts stable';
  }

  sessionsBadgeClass(): string {
    return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
  }

  sessionsTrendCaption(growth: number): string {
    if (this.growthTone(growth) === 'neutral') {
      return 'No week-over-week delta';
    }
    return 'Week-over-week';
  }

  permissionLabel(permission: string): string {
    return permission
      .split('.')
      .map((part) => part.replace(/([a-z])([A-Z])/g, '$1 $2'))
      .join(' / ');
  }

  get permissionShare(): number {
    if (!this.overview?.totalRoles) {
      return 0;
    }

    return 100 / this.overview.totalRoles;
  }

  visibleTrend(points: DashboardTrendPoint[]): DashboardTrendPoint[] {
    return points.slice(-this.selectedTrendWindow);
  }

  trendLabelVisible(index: number, total: number): boolean {
    if (total <= 7) {
      return true;
    }

    const lastIndex = total - 1;
    const interval = this.selectedTrendWindow === 30 ? 5 : 3;
    return index === 0 || index === lastIndex || index % interval === 0;
  }

  get visibleLoginTrend(): DashboardTrendPoint[] {
    return this.overview ? this.visibleTrend(this.overview.loginTrend) : [];
  }

  get visibleNewUsersTrend(): DashboardTrendPoint[] {
    return this.overview ? this.visibleTrend(this.overview.newUsersTrend) : [];
  }

  trendMax(points: DashboardTrendPoint[]): number {
    const values = this.visibleTrend(points).map((point) => point.value);
    return Math.max(1, ...values);
  }

  trendHeight(point: DashboardTrendPoint, points: DashboardTrendPoint[]): number {
    const max = this.trendMax(points);
    return Math.max(8, Math.round((point.value / max) * 100));
  }

  trendTone(value: number): 'positive' | 'negative' | 'neutral' {
    if (value > 0) {
      return 'positive';
    }
    if (value < 0) {
      return 'negative';
    }
    return 'neutral';
  }

  statusBadgeClass(value: boolean): string {
    return value
      ? 'bg-success-50 text-success-700 ring-1 ring-success-200/80 dark:bg-success-900/40 dark:text-success-300 dark:ring-success-800/80'
      : 'bg-danger-50 text-danger-700 ring-1 ring-danger-200/80 dark:bg-danger-900/40 dark:text-danger-300 dark:ring-danger-800/80';
  }

  statusDotClass(value: boolean): string {
    return value ? 'bg-success-500' : 'bg-danger-500';
  }

  severityBadgeClass(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'bg-danger-50 text-danger-700 ring-1 ring-danger-200/80 dark:bg-danger-900/40 dark:text-danger-300 dark:ring-danger-800/80';
      case 'high':
        return 'bg-warning-50 text-warning-700 ring-1 ring-warning-200/80 dark:bg-warning-900/40 dark:text-warning-300 dark:ring-warning-800/80';
      case 'medium':
        return 'bg-warning-50 text-warning-700 ring-1 ring-warning-200/80 dark:bg-warning-900/40 dark:text-warning-300 dark:ring-warning-800/80';
      default:
        return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
    }
  }

  actionTone(action: DashboardAuditAction): 'positive' | 'negative' | 'neutral' {
    if (action.type.toLowerCase().includes('deleted')) {
      return 'negative';
    }
    if (action.type.toLowerCase().includes('created') || action.type.toLowerCase().includes('updated')) {
      return 'positive';
    }
    return 'neutral';
  }

  actionIconClass(action: DashboardAuditAction): string {
    switch (this.actionTone(action)) {
      case 'positive':
        return 'bg-success-50 text-success-600 dark:bg-success-900/40 dark:text-success-300';
      case 'negative':
        return 'bg-danger-50 text-danger-600 dark:bg-danger-900/40 dark:text-danger-300';
      default:
        return 'bg-panel-muted text-soft';
    }
  }

  permissionCardClass(card: DashboardPermissionCard): string {
    if (!this.overview?.totalRoles) {
      return 'bg-panel-muted text-ink-secondary';
    }

    const share = card.rolesGranted / this.overview.totalRoles;

    if (share >= 0.75) {
      return 'bg-success-50 text-success-700 ring-1 ring-success-200/80 dark:bg-success-900/40 dark:text-success-300 dark:ring-success-800/80';
    }

    if (share >= 0.4) {
      return 'bg-primary-50 text-primary-700 ring-1 ring-primary-200/80 dark:bg-primary-900/40 dark:text-primary-300 dark:ring-primary-800/80';
    }

    if (share > 0) {
      return 'bg-warning-50 text-warning-700 ring-1 ring-warning-200/80 dark:bg-warning-900/40 dark:text-warning-300 dark:ring-warning-800/80';
    }

    return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
  }
}
