import { Component, Input, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SecurityAlertsService } from '../../../features/security-alerts/security-alerts.service';
import { BrandLogoComponent } from '../brand-logo/brand-logo.component';
import { NAVIGATION_ITEMS, NavigationItem } from '../../constants/navigation-catalog';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, BrandLogoComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent implements OnInit {
  @Input() collapsed = false;

  readonly securityAlertsCount = signal<number>(0);
  readonly showBlockedItems = signal<boolean>(false);

  readonly menuItems: NavigationItem[] = NAVIGATION_ITEMS;

  readonly groupLabels: Record<string, string> = {
    overview: 'Overview',
    administration: 'Administration',
    security: 'Security',
    account: 'Account'
  };

  constructor(
    private readonly authService: AuthService,
    private readonly securityAlertsService: SecurityAlertsService
  ) {}

  ngOnInit(): void {
    const canSeeAlerts = this.authService.hasPermission('SecurityEvents.View');

    if (!canSeeAlerts) {
      return;
    }

    this.securityAlertsService
      .getSecurityAlerts(1, 1, { type: '', userId: '', severity: '', status: '', fromDate: '', toDate: '' })
      .subscribe({
        next: (response) => this.securityAlertsCount.set(response.totalCount),
        error: () => this.securityAlertsCount.set(0)
      });

    // Check if user is admin (has any admin permission)
    const isAdmin = this.authService.hasPermission('Users.View') && this.authService.hasPermission('Roles.View');
    this.showBlockedItems.set(isAdmin);
  }

  badgeCount(item: NavigationItem): number {
    return item.badge === 'securityAlerts' ? this.securityAlertsCount() : 0;
  }

  canAccessItem(item: NavigationItem): boolean {
    if (!item.requiredAny.length) {
      return true;
    }

    return item.requiredAny.some((permission) => this.authService.hasPermission(permission));
  }

  get groupedMenuItems(): Record<string, NavigationItem[]> {
    const groups: Record<string, NavigationItem[]> = {
      overview: [],
      administration: [],
      security: [],
      account: []
    };

    this.menuItems.forEach((item) => {
      // Add accessible items
      if (this.canAccessItem(item)) {
        groups[item.group].push(item);
      } else if (this.showBlockedItems()) {
        // Add blocked items for admin view
        groups[item.group].push(item);
      }
    });

    return groups;
  }

  get visibleGroups(): string[] {
    return Object.keys(this.groupedMenuItems).filter((group) => this.groupedMenuItems[group].length > 0);
  }
}
