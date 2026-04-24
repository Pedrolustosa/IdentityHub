import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { DashboardService, DashboardSummary } from '../../dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  summary: DashboardSummary | null = null;

  readonly tooltipTotalUsers =
    'The headline is the count of all users. The badge shows week-over-week change in new registrations (accounts created in the last 7 days vs the previous 7 days, UTC). It does not represent growth of total headcount. Same rules as GET /api/Dashboard.';

  readonly tooltipActiveSessions =
    'Count of rows in UserSessions with IsActive = true. SessionsGrowthPercent from the API is currently always 0, so the trend badge stays neutral until a week-over-week comparison is implemented.';

  readonly tooltipNewUsers =
    'Users whose CreatedAt falls on or after UtcNow minus 7 days. The badge reuses the same signup momentum percentage as the first card (new users this week vs the week before).';

  readonly tooltipSecurityAlerts =
    'Count of SecurityEvents in the rolling last 7 days (UTC). For this card, a rising percentage (more events than the prior week) is shown as a risk tone; fewer events than before is shown as a positive tone.';

  constructor(
    private readonly dashboardService: DashboardService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.loadError = null;

    this.dashboardService
      .getSummary()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (data) => {
          this.summary = data;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.summary = null;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Dashboard');
        }
      });
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat(undefined, { maximumFractionDigits: 0 }).format(value);
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
        return 'bg-emerald-50 text-emerald-800 ring-1 ring-emerald-200/80';
      case 'negative':
        return 'bg-rose-50 text-rose-800 ring-1 ring-rose-200/80';
      default:
        return 'bg-slate-100 text-slate-600 ring-1 ring-slate-200/80';
    }
  }

  alertsTrendBadgeClass(value: number): string {
    switch (this.growthTone(value)) {
      case 'positive':
        return 'bg-rose-50 text-rose-800 ring-1 ring-rose-200/80';
      case 'negative':
        return 'bg-emerald-50 text-emerald-800 ring-1 ring-emerald-200/80';
      default:
        return 'bg-slate-100 text-slate-600 ring-1 ring-slate-200/80';
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
    return 'bg-slate-100 text-slate-600 ring-1 ring-slate-200/80';
  }

  sessionsTrendCaption(growth: number): string {
    if (this.growthTone(growth) === 'neutral') {
      return 'No week-over-week delta';
    }
    return 'Week-over-week';
  }
}
