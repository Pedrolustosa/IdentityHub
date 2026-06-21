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
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-security-alerts',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LoadErrorBannerComponent],
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
        return 'bg-rose-50 text-rose-700 ring-1 ring-rose-200/80';
      case 'reviewed':
        return 'bg-amber-50 text-amber-700 ring-1 ring-amber-200/80';
      case 'resolved':
        return 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200/80';
      case 'ignored':
        return 'bg-slate-100 text-slate-700 ring-1 ring-slate-200/80';
      default:
        return 'bg-slate-100 text-slate-700 ring-1 ring-slate-200/80';
    }
  }

  severityBadgeClass(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'bg-rose-100 text-rose-800 ring-1 ring-rose-300/90';
      case 'high':
        return 'bg-orange-100 text-orange-800 ring-1 ring-orange-300/90';
      case 'medium':
        return 'bg-amber-100 text-amber-800 ring-1 ring-amber-300/90';
      default:
        return 'bg-emerald-100 text-emerald-800 ring-1 ring-emerald-300/90';
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
