import { Component, Input, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SecurityAlertsService } from '../../../features/security-alerts/security-alerts.service';
import { BrandLogoComponent } from '../brand-logo/brand-logo.component';

type SidebarMenuItem = {
  label: string;
  route: string;
  icon: string;
  permission?: string;
  permissions?: string[];
  requireAll?: boolean;
  badge?: 'securityAlerts';
  group: 'overview' | 'administration' | 'security' | 'account';
};

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

  readonly menuItems: SidebarMenuItem[] = [
    // Overview
    { label: 'Dashboard', route: '/app/dashboard', icon: 'dashboard', permission: 'Dashboard.View', group: 'overview' },

    // Administration
    { label: 'Users', route: '/app/users', icon: 'users', permission: 'Users.View', group: 'administration' },
    {
      label: 'Roles & Permissions',
      route: '/app/roles',
      icon: 'roleClaims',
      permission: 'Roles.View',
      group: 'administration'
    },

    // Security
    { label: 'My Sessions', route: '/app/profile', icon: 'sessions', permission: '', group: 'security' },
    {
      label: 'Security Alerts',
      route: '/app/security-alerts',
      icon: 'securityAlerts',
      permission: 'SecurityEvents.View',
      badge: 'securityAlerts',
      group: 'security'
    },
    { label: 'Audit Logs', route: '/app/audit-logs', icon: 'auditLogs', permission: 'Audit.View', group: 'security' },
    { label: 'System Sessions', route: '/app/sessions', icon: 'sessions', permissions: ['Sessions.View', 'Users.View'], group: 'security' },
    { label: 'Recent Activity', route: '/app/activity', icon: 'auditLogs', permissions: ['Activity.View', 'Audit.View'], group: 'security' },
    { label: 'Security Settings', route: '/app/security-settings', icon: 'securityAlerts', permissions: ['SecuritySettings.View', 'SecurityEvents.Manage'], group: 'security' },

    // Account
    { label: 'My Access', route: '/app/my-access', icon: 'access', permission: '', group: 'account' },
    { label: 'User Invites', route: '/app/user-invites', icon: 'users', permissions: ['Users.Invites.View', 'Users.View'], group: 'account' },
    { label: 'Permissions Matrix', route: '/app/permissions/matrix', icon: 'roleClaims', permission: 'Roles.Permissions.View', group: 'account' },
    { label: 'Permissions Catalog', route: '/app/permissions/catalog', icon: 'roleClaims', permission: 'Roles.Permissions.View', group: 'account' }
  ];

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

  badgeCount(item: SidebarMenuItem): number {
    return item.badge === 'securityAlerts' ? this.securityAlertsCount() : 0;
  }

  canAccessItem(item: SidebarMenuItem): boolean {
    const single = item.permission ? [item.permission] : [];
    const many = item.permissions ?? [];
    const required = [...new Set([...single, ...many])];

    if (!required.length) {
      return true;
    }

    if (item.requireAll) {
      return required.every((permission) => this.authService.hasPermission(permission));
    }

    return required.some((permission) => this.authService.hasPermission(permission));
  }

  get groupedMenuItems(): Record<string, SidebarMenuItem[]> {
    const groups: Record<string, SidebarMenuItem[]> = {
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
