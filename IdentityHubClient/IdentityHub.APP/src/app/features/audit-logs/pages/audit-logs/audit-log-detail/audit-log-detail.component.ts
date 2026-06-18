import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuditLogItem, AuditLogsService } from '../../../audit-logs.service';
import { LoadErrorBannerComponent } from '../../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-audit-log-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './audit-log-detail.component.html',
  styleUrl: './audit-log-detail.component.css'
})
export class AuditLogDetailComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  auditLog: AuditLogItem | null = null;
  auditLogId = '';
  copied = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly auditLogsService: AuditLogsService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/audit-logs']);
      return;
    }

    this.auditLogId = id;
    this.loadAuditLog(id);
  }

  loadAuditLog(id: string): void {
    this.isLoading = true;
    this.loadError = null;

    this.auditLogsService
      .getById(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (item) => {
          this.auditLog = item;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Audit Logs');
        }
      });
  }

  get metadataPretty(): string {
    const raw = this.auditLog?.metadataJson?.trim();
    if (!raw) {
      return 'No metadata available.';
    }

    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw;
    }
  }

  copyId(): void {
    if (!this.auditLog || typeof navigator === 'undefined') {
      return;
    }

    navigator.clipboard.writeText(this.auditLog.id).then(() => {
      this.copied = true;
      setTimeout(() => (this.copied = false), 1200);
    }).catch(() => {});
  }
}
