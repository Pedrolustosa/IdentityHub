import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService, UserSessionResponse } from '../../../../core/services/auth.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { CriticalActionConfirmationService } from '../../../../shared/services/critical-action-confirmation.service';

@Component({
  selector: 'app-my-sessions',
  standalone: true,
  imports: [CommonModule, UxStateComponent],
  templateUrl: './my-sessions.component.html',
  styleUrl: './my-sessions.component.css'
})
export class MySessionsComponent implements OnInit {
  isSessionsLoading = false;
  sessionsLoadError: UiLoadError | null = null;
  sessions: UserSessionResponse[] = [];
  sessionsHistory: UserSessionResponse[] = [];
  isSessionsHistoryLoading = false;
  sessionsHistoryLoadError: UiLoadError | null = null;
  revokingSessionId: string | null = null;
  revokingOtherSessions = false;

  constructor(
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadSessions();
    this.loadSessionsHistory();
  }

  loadSessions(): void {
    this.isSessionsLoading = true;
    this.sessionsLoadError = null;

    this.authService
      .getSessions()
      .pipe(finalize(() => (this.isSessionsLoading = false)))
      .subscribe({
        next: (sessions) => {
          this.sessions = sessions;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  loadSessionsHistory(): void {
    this.isSessionsHistoryLoading = true;
    this.sessionsHistoryLoadError = null;

    this.authService
      .getSessionsHistory(20)
      .pipe(finalize(() => (this.isSessionsHistoryLoading = false)))
      .subscribe({
        next: (history) => {
          this.sessionsHistory = history;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsHistoryLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Login history');
        }
      });
  }

  revokeSession(session: UserSessionResponse): void {
    if (this.revokingSessionId) {
      return;
    }

    const confirmed = session.isCurrent
      ? this.criticalActionConfirmationService.confirmRevokeCurrentSession()
      : this.criticalActionConfirmationService.confirmRevokeSession();
    if (!confirmed) {
      return;
    }

    this.revokingSessionId = session.id;
    this.sessionsLoadError = null;

    this.authService
      .revokeSession(session.id)
      .pipe(finalize(() => (this.revokingSessionId = null)))
      .subscribe({
        next: () => {
          if (session.isCurrent) {
            this.toastr.success('Session revoked. Please sign in again.', 'Sessions');
            this.authService.clearClientSessionAndNavigateToLogin();
            return;
          }

          this.sessions = this.sessions.filter((entry) => entry.id !== session.id);
          this.sessionsHistory = this.sessionsHistory.map((entry) =>
            entry.id === session.id
              ? {
                  ...entry,
                  isActive: false,
                  revokedAt: new Date().toISOString()
                }
              : entry
          );
          this.toastr.success('Session revoked.', 'Sessions');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  isRevokingSession(sessionId: string): boolean {
    return this.revokingSessionId === sessionId;
  }

  revokeAllOtherSessions(): void {
    if (this.revokingOtherSessions) {
      return;
    }

    const hasOtherSessions = this.sessions.some((session) => !session.isCurrent);
    if (!hasOtherSessions) {
      this.toastr.info('No other active sessions to revoke.', 'Sessions');
      return;
    }

    const confirmed = this.criticalActionConfirmationService.confirmRevokeOtherSessions();
    if (!confirmed) {
      return;
    }

    this.revokingOtherSessions = true;
    this.sessionsLoadError = null;

    this.authService
      .revokeOtherSessions()
      .pipe(finalize(() => (this.revokingOtherSessions = false)))
      .subscribe({
        next: () => {
          this.sessions = this.sessions.filter((session) => session.isCurrent);
          this.loadSessionsHistory();
          this.toastr.success('All other sessions were revoked.', 'Sessions');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  currentSessionsCount(): number {
    return this.sessions.filter((session) => session.isCurrent).length;
  }

  historyActiveCount(): number {
    return this.sessionsHistory.filter((session) => session.isActive).length;
  }

  historyRevokedCount(): number {
    return this.sessionsHistory.filter((session) => !session.isActive).length;
  }

  sessionStatusClass(session: UserSessionResponse): string {
    if (session.isCurrent) {
      return 'bg-success-50 text-success-700 ring-1 ring-success-200 dark:bg-success-900/40 dark:text-success-300 dark:ring-success-800';
    }

    if (session.isActive) {
      return 'bg-primary-50 text-primary-700 ring-1 ring-primary-200 dark:bg-primary-900/40 dark:text-primary-300 dark:ring-primary-800';
    }

    return 'bg-panel-muted text-ink-secondary ring-1 ring-line';
  }

  sessionStatusText(session: UserSessionResponse): string {
    if (session.isCurrent) {
      return 'Current';
    }

    return session.isActive ? 'Active' : 'Revoked';
  }
}
