import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { BrandLogoComponent } from '../brand-logo/brand-logo.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, BrandLogoComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  @Input() collapsed = false;

  readonly menuItems = [
    { label: 'Dashboard', route: '/app/dashboard', icon: 'dashboard' },
    { label: 'Users', route: '/app/users', icon: 'users' },
    { label: 'Role claims', route: '/app/role-claims', icon: 'roleClaims' }
  ];
}
