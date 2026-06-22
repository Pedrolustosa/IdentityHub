import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../../core/services/auth.service';
import { UxStateComponent } from '../../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { CriticalActionConfirmationService } from '../../../../../shared/services/critical-action-confirmation.service';
import { UserAuditItem, UserListItem, UserSessionItem, UsersService } from '../../../users.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, UxStateComponent],
  templateUrl: './user-detail.component.html',
  styleUrl: './user-detail.component.css'
})
export class UserDetailComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  user: UserListItem | null = null;
  userId = '';
  isRevokingSessions = false;
  isResendingPasswordReset = false;
  selectedTab: 'summary' | 'sessions' | 'audit' = 'summary';
  sessions: UserSessionItem[] = [];
  sessionHistory: UserSessionItem[] = [];
  sessionsLoaded = false;
  isLoadingSessions = false;
  auditItems: UserAuditItem[] = [];
  auditLoaded = false;
  isLoadingAudit = false;
  readonly canEditUser: boolean;
  readonly canRevokeUserSessions: boolean;
  readonly currentUserId: string | null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly usersService: UsersService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canEditUser = this.authService.hasPermission('Users.Update');
    this.canRevokeUserSessions = this.authService.hasPermission('Sessions.Revoke');
    this.currentUserId = this.authService.getCurrentUserId();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/users']);
      return;
    }
    this.userId = id;
    this.loadUser(id);
  }

  loadUser(id: string): void {
    this.isLoading = true;
    this.loadError = null;
    this.user = null;

    this.usersService
      .getUserById(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (u) => {
          this.user = u;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  get isViewingSelf(): boolean {
    return !!this.currentUserId && this.currentUserId === this.userId;
  }

  setTab(tab: 'summary' | 'sessions' | 'audit'): void {
    this.selectedTab = tab;
    if (tab === 'sessions' && !this.sessionsLoaded) {
      this.loadSessions();
    }
    if (tab === 'audit' && !this.auditLoaded) {
      this.loadAudit();
    }
  }

  tabClass(tab: 'summary' | 'sessions' | 'audit'): string {
    return this.selectedTab === tab
      ? 'bg-white text-slate-900 shadow-sm'
      : 'text-slate-500 hover:text-slate-900';
  }

  loadSessions(): void {
    this.isLoadingSessions = true;
    this.usersService
      .getUserSessionsHistory(this.userId, 30)
      .pipe(finalize(() => (this.isLoadingSessions = false)))
      .subscribe({
        next: (sessions) => {
          this.sessionHistory = sessions;
          this.sessions = sessions.filter((session) => session.isActive !== false);
          this.sessionsLoaded = true;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  loadAudit(): void {
    this.isLoadingAudit = true;
    this.usersService
      .getUserAudit(this.userId, 30)
      .pipe(finalize(() => (this.isLoadingAudit = false)))
      .subscribe({
        next: (items) => {
          this.auditItems = items;
          this.auditLoaded = true;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Audit');
        }
      });
  }

  revokeAllSessions(): void {
    if (this.isRevokingSessions || !this.user) {
      return;
    }

    if (!this.criticalActionConfirmationService.confirmRevokeUserSessions(this.user.email ?? 'this user')) {
      return;
    }

    this.isRevokingSessions = true;
    this.authService
      .revokeUserSessions(this.userId)
      .pipe(finalize(() => (this.isRevokingSessions = false)))
      .subscribe({
        next: () => {
          this.toastr.success('All sessions revoked.', 'Users');
          this.sessions = [];
          this.sessionHistory = this.sessionHistory.map((session) => ({
            ...session,
            isActive: false,
            revokedAt: session.revokedAt ?? new Date().toISOString()
          }));
          this.sessionsLoaded = false;
          this.loadUser(this.userId);
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  sendPasswordReset(): void {
    if (this.isResendingPasswordReset || !this.user?.email) {
      return;
    }

    this.isResendingPasswordReset = true;
    this.authService
      .forgotPassword({ email: this.user.email })
      .pipe(finalize(() => (this.isResendingPasswordReset = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Password reset email sent.', 'Users');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  historyActiveCount(): number {
    return this.sessionHistory.filter((session) => session.isActive !== false).length;
  }

  historyRevokedCount(): number {
    return this.sessionHistory.filter((session) => session.isActive === false).length;
  }
}
