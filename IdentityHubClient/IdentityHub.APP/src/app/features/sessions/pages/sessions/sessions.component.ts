import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import {
  AdminSessionFilters,
  AdminSessionItem,
  PagedAdminSessions,
  SessionsService
} from '../../sessions.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { CriticalActionConfirmationService } from '../../../../shared/services/critical-action-confirmation.service';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, UxStateComponent],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.css'
})
export class SessionsComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  sessions: AdminSessionItem[] = [];
  page = 1;
  readonly pageSize = 20;
  totalCount = 0;
  totalPages = 0;
  revokingSessionId: string | null = null;
  readonly canRevokeSessions: boolean;
  readonly filters: AdminSessionFilters = {
    activeOnly: true,
    search: '',
    userId: ''
  };

  constructor(
    private readonly sessionsService: SessionsService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canRevokeSessions = this.authService.hasPermission('Sessions.Revoke');
  }

  ngOnInit(): void {
    this.load();
  }

  load(page = this.page): void {
    this.isLoading = true;
    this.loadError = null;

    this.sessionsService
      .getSessions(page, this.pageSize, this.filters)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response: PagedAdminSessions) => {
          this.sessions = response.items;
          this.page = response.page;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'System sessions');
        }
      });
  }

  applyFilters(): void {
    this.load(1);
  }

  resetFilters(): void {
    this.filters.activeOnly = true;
    this.filters.search = '';
    this.filters.userId = '';
    this.load(1);
  }

  goToPage(page: number): void {
    if (page < 1 || (this.totalPages > 0 && page > this.totalPages) || page === this.page) {
      return;
    }

    this.load(page);
  }

  revokeSession(session: AdminSessionItem): void {
    if (!this.canRevokeSessions || this.revokingSessionId || !session.isActive) {
      return;
    }

    const label = session.email || session.fullName || session.userId;
    const confirmed = session.isCurrent
      ? this.criticalActionConfirmationService.confirmRevokeCurrentSession()
      : this.criticalActionConfirmationService.confirmRevokeSessionForUser(label);

    if (!confirmed) {
      return;
    }

    this.revokingSessionId = session.id;

    this.sessionsService
      .revokeSession(session.id)
      .pipe(finalize(() => (this.revokingSessionId = null)))
      .subscribe({
        next: () => {
          if (session.isCurrent) {
            this.toastr.success('Current session revoked. Please sign in again.', 'System sessions');
            this.authService.clearClientSessionAndNavigateToLogin();
            return;
          }

          this.toastr.success('Session revoked.', 'System sessions');
          this.load(this.page);
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'System sessions');
        }
      });
  }
}
