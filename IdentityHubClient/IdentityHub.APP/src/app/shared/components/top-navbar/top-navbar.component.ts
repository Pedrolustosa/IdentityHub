import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
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

  logout(): void {
    this.authService.logout();
  }
}
