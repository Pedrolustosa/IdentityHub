import { Component, EventEmitter, OnInit, Output, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService, UserSessionResponse } from '../../../core/services/auth.service';
import { BirthdayGreetingService } from '../../../core/services/birthday-greeting.service';
import { ThemeService } from '../../../core/services/theme.service';
import { BreadcrumbService } from '../../../core/services/breadcrumb.service';
import { SecurityAlertsService } from '../../../features/security-alerts/security-alerts.service';
import { getEnvironmentBadge } from '../../constants/environment-badge';
import { isDateOfBirthToday } from '../../utils/birthday.util';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-top-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './top-navbar.component.html',
  styleUrl: './top-navbar.component.css'
})
export class TopNavbarComponent implements OnInit {
  @Output() sidebarToggle = new EventEmitter<void>();
  isUserMenuOpen = false;
  displayName = 'User';
  userEmail = '';
  currentSession = signal<UserSessionResponse | null>(null);
  notificationCount = signal<number>(0);
  isLoadingSession = false;
  showLogoutConfirm = false;

  readonly envBadge = getEnvironmentBadge();
  private readonly themeService = inject(ThemeService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly birthdayGreeting = inject(BirthdayGreetingService);
  readonly pageTitle = this.breadcrumbService.pageTitle;
  readonly theme = this.themeService.theme;
  readonly isBirthdayToday = this.birthdayGreeting.isBirthdayToday;

  constructor(
    private readonly authService: AuthService,
    private readonly securityAlertsService: SecurityAlertsService
  ) {
    this.displayName = this.authService.getCurrentUserDisplayName();
  }

  ngOnInit(): void {
    this.authService.getMe().subscribe({
      next: (me) => {
        const name = (me.fullName ?? '').trim();
        if (name) {
          this.displayName = name;
        }
        this.userEmail = me.email ?? '';
        this.birthdayGreeting.isBirthdayToday.set(isDateOfBirthToday(me.dateOfBirth));
      }
    });

    this.loadCurrentSession();
    this.loadNotificationCount();
  }

  openBirthdayCelebration(): void {
    this.birthdayGreeting.openCelebration();
  }

  private loadCurrentSession(): void {
    this.isLoadingSession = true;
    this.authService
      .getSessions()
      .pipe(finalize(() => (this.isLoadingSession = false)))
      .subscribe({
        next: (sessions) => {
          const current = sessions.find((s) => s.isCurrent);
          this.currentSession.set(current ?? null);
        }
      });
  }

  private loadNotificationCount(): void {
    if (!this.authService.hasPermission('SecurityEvents.View')) {
      this.notificationCount.set(0);
      return;
    }

    this.securityAlertsService.getUnreadCount().subscribe({
      next: (count) => this.notificationCount.set(count),
      error: () => this.notificationCount.set(0)
    });
  }

  getInitials(): string {
    return this.displayName
      .split(' ')
      .map((word) => word.charAt(0).toUpperCase())
      .join('')
      .slice(0, 2);
  }

  getLastAccessTimeDisplay(): string {
    const session = this.currentSession();
    if (!session?.lastAccessAt) {
      return 'Agora';
    }
    const date = new Date(session.lastAccessAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Agora';
    if (diffMins < 60) return `${diffMins} min atrás`;

    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours}h atrás`;

    return date.toLocaleDateString('pt-BR', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  toggleSidebar(): void {
    this.sidebarToggle.emit();
  }

  toggleTheme(): void {
    this.themeService.toggle();
  }

  confirmLogout(): void {
    this.showLogoutConfirm = true;
  }

  cancelLogout(): void {
    this.showLogoutConfirm = false;
  }

  logout(): void {
    this.authService.logout();
  }
}
