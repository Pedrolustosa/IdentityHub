import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../../core/services/auth.service';
import { LoadErrorBannerComponent } from '../../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { SecurityAlertItem, SecurityAlertsService } from '../../../security-alerts.service';

@Component({
  selector: 'app-security-alert-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadErrorBannerComponent],
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
        return 'bg-rose-50 text-rose-700 ring-1 ring-rose-200/80';
      case 'reviewed':
        return 'bg-amber-50 text-amber-700 ring-1 ring-amber-200/80';
      case 'resolved':
        return 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200/80';
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
}
