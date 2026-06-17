import { Component, EventEmitter, OnInit, Output, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { BreadcrumbService } from '../../../core/services/breadcrumb.service';
import { getEnvironmentBadge } from '../../constants/environment-badge';
import { CommonModule } from '@angular/common';

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

  readonly envBadge = getEnvironmentBadge();
  private readonly themeService = inject(ThemeService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  readonly pageTitle = this.breadcrumbService.pageTitle;
  readonly theme = this.themeService.theme;

  constructor(private readonly authService: AuthService) {
    this.displayName = this.authService.getCurrentUserDisplayName();
  }

  ngOnInit(): void {
    this.authService.getMe().subscribe({
      next: (me) => {
        const name = (me.fullName ?? '').trim();
        if (name) {
          this.displayName = name;
        }
      }
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

  logout(): void {
    this.authService.logout();
  }
}
