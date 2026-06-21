import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuditLogItem, AuditLogsService } from '../../../audit-logs/audit-logs.service';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs/operators';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule, LoadErrorBannerComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Recent activity</h1>
        <p class="text-slate-600">Friendly timeline of recent audit events.</p>
      </header>

      @if (isLoading) {
        <div class="rounded-xl border bg-white p-6 shadow-sm">
          <div class="h-8 w-64 animate-pulse rounded bg-slate-100"></div>
        </div>
      } @else if (loadError) {
        <div class="rounded-xl border bg-white p-4 shadow-sm">
          <app-load-error-banner [error]="loadError" (retry)="load()" />
        </div>
      } @else if (items.length === 0) {
        <div class="rounded-xl border bg-white p-6 text-sm text-slate-500 shadow-sm">No recent activity.</div>
      } @else {
        <div class="space-y-2">
          @for (item of items; track item.id) {
            <article class="rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
              <div class="flex flex-col gap-1 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p class="text-sm font-semibold text-slate-900">{{ item.type }}</p>
                  <p class="text-sm text-slate-700">{{ item.description }}</p>
                  <p class="text-xs text-slate-500 mt-1">Actor: {{ item.actorUserId }}</p>
                </div>
                <p class="text-xs text-slate-500 whitespace-nowrap">{{ item.createdAt | date:'medium' }}</p>
              </div>
            </article>
          }
        </div>
      }
    </section>
  `
})
export class ActivityComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  items: AuditLogItem[] = [];

  constructor(
    private readonly auditLogsService: AuditLogsService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    this.auditLogsService
      .getAuditLogs(1, 15, { type: '', actorUserId: '', description: '', fromDate: '', toDate: '' })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.items = response.items;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Recent activity');
        }
      });
  }
}
