import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuditLogFilters, AuditLogItem, AuditLogsService, PagedAuditLogs } from '../../audit-logs.service';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadErrorBannerComponent],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.css'
})
export class AuditLogsComponent implements OnInit {
  readonly eventTypeOptions = [
    'Audit.User.Created',
    'Audit.User.Updated',
    'Audit.User.Deleted',
    'Audit.User.RolesUpdated',
    'Audit.Role.Created',
    'Audit.Role.Updated',
    'Audit.Role.Deleted',
    'Audit.Role.PermissionsUpdated',
    'Audit.RoleClaim.Added',
    'Audit.RoleClaim.Removed',
    'Audit.RoleClaim.Replaced'
  ];

  isLoading = true;
  isExporting = false;
  loadError: UiLoadError | null = null;
  items: AuditLogItem[] = [];
  page = 1;
  readonly pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  readonly filters: AuditLogFilters = {
    type: '',
    actorUserId: '',
    description: '',
    fromDate: '',
    toDate: ''
  };

  constructor(
    private readonly auditLogsService: AuditLogsService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  loadAuditLogs(page = this.page): void {
    this.isLoading = true;
    this.loadError = null;

    this.auditLogsService
      .getAuditLogs(page, this.pageSize, this.filters)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response: PagedAuditLogs) => {
          this.items = response.items;
          this.page = response.page;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Audit Logs');
        }
      });
  }

  applyFilters(): void {
    this.loadAuditLogs(1);
  }

  resetFilters(): void {
    this.filters.type = '';
    this.filters.actorUserId = '';
    this.filters.description = '';
    this.filters.fromDate = '';
    this.filters.toDate = '';
    this.loadAuditLogs(1);
  }

  exportCsv(): void {
    this.isExporting = true;

    this.auditLogsService
      .exportAuditLogsCsv(this.filters)
      .pipe(finalize(() => (this.isExporting = false)))
      .subscribe({
        next: (blob) => {
          const objectUrl = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = objectUrl;
          link.download = `audit-logs-${new Date().toISOString().replace(/[:.]/g, '-')}.csv`;
          link.click();
          URL.revokeObjectURL(objectUrl);
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Audit Logs');
        }
      });
  }

  previousPage(): void {
    if (this.page <= 1 || this.isLoading) {
      return;
    }

    this.loadAuditLogs(this.page - 1);
  }

  nextPage(): void {
    if (this.page >= this.totalPages || this.isLoading) {
      return;
    }

    this.loadAuditLogs(this.page + 1);
  }

  trackById(_: number, item: AuditLogItem): string {
    return item.id;
  }
}
