import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
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
  imports: [CommonModule, FormsModule, LoadErrorBannerComponent],
  templateUrl: './security-alerts.component.html',
  styleUrl: './security-alerts.component.css'
})
export class SecurityAlertsComponent implements OnInit {
  readonly eventTypeOptions = SECURITY_ALERT_EVENT_TYPE_OPTIONS;

  isLoading = true;
  loadError: UiLoadError | null = null;
  items: SecurityAlertItem[] = [];
  page = 1;
  readonly pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  readonly filters: SecurityAlertFilters = {
    type: '',
    userId: '',
    fromDate: '',
    toDate: ''
  };

  constructor(
    private readonly securityAlertsService: SecurityAlertsService,
    private readonly toastr: ToastrService
  ) {}

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
    this.filters.fromDate = '';
    this.filters.toDate = '';
    this.loadSecurityAlerts(1);
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
    if (this.filters.fromDate.trim()) {
      count++;
    }
    if (this.filters.toDate.trim()) {
      count++;
    }

    return count;
  }
}