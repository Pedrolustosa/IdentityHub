import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { BrandLogoComponent } from '../brand-logo/brand-logo.component';

type SidebarMenuItem = {
  label: string;
  route: string;
  icon: string;
  permission?: string;
  permissions?: string[];
  requireAll?: boolean;
};

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, BrandLogoComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  @Input() collapsed = false;

  readonly menuItems: SidebarMenuItem[] = [
    { label: 'Dashboard', route: '/app/dashboard', icon: 'dashboard', permission: 'Dashboard.View' },
    { label: 'My Access', route: '/app/my-access', icon: 'profile' },
    { label: 'Users', route: '/app/users', icon: 'users', permission: 'Users.View' },
    {
      label: 'Role Permissions',
      route: '/app/roles',
      icon: 'roleClaims',
      permission: 'Roles.View'
    },
    { label: 'Audit Logs', route: '/app/audit-logs', icon: 'auditLogs', permission: 'Audit.View' },
    { label: 'Security Alerts', route: '/app/security-alerts', icon: 'securityAlerts', permissions: ['SecurityEvents.View', 'Audit.View'] }
  ];

  constructor(private readonly authService: AuthService) {}

  get visibleMenuItems(): SidebarMenuItem[] {
    return this.menuItems.filter((item) => {
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
    });
  }
}
