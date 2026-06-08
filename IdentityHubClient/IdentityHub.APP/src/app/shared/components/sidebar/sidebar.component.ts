import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { BrandLogoComponent } from '../brand-logo/brand-logo.component';

type SidebarMenuItem = {
  label: string;
  route: string;
  icon: string;
  permission?: string;
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
    { label: 'Dashboard', route: '/app/dashboard', icon: 'dashboard' },
    { label: 'Users', route: '/app/users', icon: 'users' },
    { label: 'Role Claims', route: '/app/role-claims', icon: 'roleClaims' },
    { label: 'Audit Logs', route: '/app/audit-logs', icon: 'auditLogs', permission: 'Audit.View' }
  ];

  constructor(private readonly authService: AuthService) {}

  get visibleMenuItems(): SidebarMenuItem[] {
    return this.menuItems.filter((item) => !item.permission || this.authService.hasPermission(item.permission));
  }
}
