import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UserListItem, UsersService } from '../../users.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  users: UserListItem[] = [];
  readonly canCreateUsers: boolean;
  readonly canEditUsers: boolean;

  constructor(
    private readonly usersService: UsersService,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {
    this.canCreateUsers = this.authService.hasPermission('Users.Create');
    this.canEditUsers = this.authService.hasPermission('Users.Update');
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.loadError = null;

    this.usersService
      .getUsers()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (users) => {
          this.users = users;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  rolesPreview(roles: string[] | null | undefined): string[] {
    return (roles ?? []).slice(0, 2);
  }

  extraRolesCount(roles: string[] | null | undefined): number {
    const list = roles ?? [];
    return Math.max(0, list.length - 2);
  }

  activeUsersCount(): number {
    return this.users.filter((user) => user.isActive).length;
  }

  inactiveUsersCount(): number {
    return this.users.filter((user) => !user.isActive).length;
  }
}
