import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import {
  PagedSecurityAlerts,
  SecurityAlertFilters,
  SecurityAlertItem,
  SecurityAlertsService
} from '../../security-alerts.service';
import { SECURITY_ALERT_EVENT_TYPE_OPTIONS } from '../../../../shared/constants/security-alert-event-types';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { CriticalActionConfirmationService } from '../../../../shared/services/critical-action-confirmation.service';

@Component({
  selector: 'app-security-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, UxStateComponent],
  templateUrl: './security-alerts.component.html',
  styleUrl: './security-alerts.component.css'
})
export class SecurityAlertsComponent implements OnInit {
  readonly eventTypeOptions = SECURITY_ALERT_EVENT_TYPE_OPTIONS;
  readonly severityOptions = ['Low', 'Medium', 'High', 'Critical'];
  readonly statusOptions = ['Open', 'Reviewed', 'Resolved', 'Ignored'];

  isLoading = true;
  loadError: UiLoadError | null = null;
  updatingAlertId: string | null = null;
  items: SecurityAlertItem[] = [];
  page = 1;
  readonly pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  readonly filters: SecurityAlertFilters = {
    type: '',
    userId: '',
    severity: '',
    status: '',
    fromDate: '',
    toDate: ''
  };
  readonly canManageAlerts: boolean;

  constructor(
    private readonly securityAlertsService: SecurityAlertsService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canManageAlerts = this.authService.hasPermission('SecurityEvents.Manage');
  }

  ngOnInit(): void {
    this.loadSecurityAlerts();
  }

  loadSecurityAlerts(page = this.page): void {
    this.isLoading = true;
    this.loadError = null;

    this.securityAlertsService
      .getSecurityAlerts(page, this.pageSize, this.filters)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response: PagedSecurityAlerts) => {
          this.items = response.items;
          this.page = response.page;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Security Alerts');
        }
      });
  }

  applyFilters(): void {
    this.loadSecurityAlerts(1);
  }

  resetFilters(): void {
    this.filters.type = '';
    this.filters.userId = '';
    this.filters.severity = '';
    this.filters.status = '';
    this.filters.fromDate = '';
    this.filters.toDate = '';
    this.loadSecurityAlerts(1);
  }

  updateStatus(item: SecurityAlertItem, status: string): void {
    if (!this.canManageAlerts || this.updatingAlertId || item.status === status) {
      return;
    }

    if (item.severity.toLowerCase() === 'critical' && status.toLowerCase() === 'resolved') {
      const confirmed = this.criticalActionConfirmationService.confirmResolveCriticalAlert(item.type);
      if (!confirmed) {
        return;
      }
    }

    this.updatingAlertId = item.id;
    this.securityAlertsService
      .updateAlertStatus(item.id, { status })
      .pipe(finalize(() => (this.updatingAlertId = null)))
      .subscribe({
        next: () => {
          this.items = this.items.map((entry) =>
            entry.id === item.id
              ? {
                  ...entry,
                  status
                }
              : entry
          );
          this.toastr.success(`Alert marked as ${status}.`, 'Security Alerts');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Security Alerts');
        }
      });
  }

  statusBadgeClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'open':
        return 'bg-danger-50 text-danger-700 ring-1 ring-danger-200/80 dark:bg-danger-900/40 dark:text-danger-300 dark:ring-danger-800/80';
      case 'reviewed':
        return 'bg-warning-50 text-warning-700 ring-1 ring-warning-200/80 dark:bg-warning-900/40 dark:text-warning-300 dark:ring-warning-800/80';
      case 'resolved':
        return 'bg-success-50 text-success-700 ring-1 ring-success-200/80 dark:bg-success-900/40 dark:text-success-300 dark:ring-success-800/80';
      case 'ignored':
        return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
      default:
        return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
    }
  }

  severityBadgeClass(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'bg-danger-100 text-danger-800 ring-1 ring-danger-300/90 dark:bg-danger-900/50 dark:text-danger-200 dark:ring-danger-700/80';
      case 'high':
        return 'bg-warning-100 text-warning-800 ring-1 ring-warning-300/90 dark:bg-warning-900/50 dark:text-warning-200 dark:ring-warning-700/80';
      case 'medium':
        return 'bg-warning-50 text-warning-700 ring-1 ring-warning-200/80 dark:bg-warning-900/40 dark:text-warning-300 dark:ring-warning-800/80';
      default:
        return 'bg-success-100 text-success-800 ring-1 ring-success-300/90 dark:bg-success-900/50 dark:text-success-200 dark:ring-success-700/80';
    }
  }

  isCritical(item: SecurityAlertItem): boolean {
    return item.severity.toLowerCase() === 'critical';
  }

  isUpdatingStatus(itemId: string): boolean {
    return this.updatingAlertId === itemId;
  }

  openAlertsCount(): number {
    return this.items.filter((item) => item.status.toLowerCase() === 'open').length;
  }

  criticalAlertsCount(): number {
    return this.items.filter((item) => item.severity.toLowerCase() === 'critical').length;
  }

  resolvedAlertsCount(): number {
    return this.items.filter((item) => item.status.toLowerCase() === 'resolved').length;
  }

  last24HoursCount(): number {
    const now = Date.now();
    const windowStart = now - 24 * 60 * 60 * 1000;
    return this.items.filter((item) => new Date(item.createdAt).getTime() >= windowStart).length;
  }

  previousPage(): void {
    if (this.page <= 1 || this.isLoading) {
      return;
    }

    this.loadSecurityAlerts(this.page - 1);
  }

  nextPage(): void {
    if (this.page >= this.totalPages || this.isLoading) {
      return;
    }

    this.loadSecurityAlerts(this.page + 1);
  }

  trackById(_: number, item: SecurityAlertItem): string {
    return item.id;
  }

  activeFiltersCount(): number {
    let count = 0;

    if (this.filters.type.trim()) {
      count++;
    }
    if (this.filters.userId.trim()) {
      count++;
    }
    if (this.filters.severity?.trim()) {
      count++;
    }
    if (this.filters.status?.trim()) {
      count++;
    }
    if (this.filters.fromDate.trim()) {
      count++;
    }
    if (this.filters.toDate.trim()) {
      count++;
    }

    return count;
  }
}
