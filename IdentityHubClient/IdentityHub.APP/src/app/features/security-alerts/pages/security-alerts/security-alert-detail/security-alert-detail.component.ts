import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../../core/services/auth.service';
import { UxStateComponent } from '../../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { CriticalActionConfirmationService } from '../../../../../shared/services/critical-action-confirmation.service';
import { SecurityAlertItem, SecurityAlertsService } from '../../../security-alerts.service';

@Component({
  selector: 'app-security-alert-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, UxStateComponent],
  templateUrl: './security-alert-detail.component.html',
  styleUrl: './security-alert-detail.component.css'
})
export class SecurityAlertDetailComponent implements OnInit {
  isLoading = true;
  isUpdating = false;
  loadError: UiLoadError | null = null;
  alert: SecurityAlertItem | null = null;
  alertId = '';
  readonly canManageAlerts: boolean;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly securityAlertsService: SecurityAlertsService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canManageAlerts = this.authService.hasPermission('SecurityEvents.Manage');
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/security-alerts']);
      return;
    }

    this.alertId = id;
    this.loadAlert(id);
  }

  loadAlert(id: string): void {
    this.isLoading = true;
    this.loadError = null;

    this.securityAlertsService
      .getById(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (item) => {
          this.alert = item;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Security Alerts');
        }
      });
  }

  updateStatus(status: string): void {
    if (!this.canManageAlerts || !this.alert || this.isUpdating || this.alert.status === status) {
      return;
    }

    if (this.alert.severity.toLowerCase() === 'critical' && status.toLowerCase() === 'resolved') {
      const confirmed = this.criticalActionConfirmationService.confirmResolveCriticalAlert(this.alert.type);
      if (!confirmed) {
        return;
      }
    }

    this.isUpdating = true;
    this.securityAlertsService
      .updateAlertStatus(this.alert.id, { status })
      .pipe(finalize(() => (this.isUpdating = false)))
      .subscribe({
        next: () => {
          if (this.alert) {
            this.alert = {
              ...this.alert,
              status
            };
          }
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
}
