import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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
  imports: [CommonModule, ReactiveFormsModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  private readonly formBuilder = new FormBuilder();

  isLoading = true;
  loadError: UiLoadError | null = null;
  inviteError: UiLoadError | null = null;
  isInviting = false;
  users: UserListItem[] = [];
  readonly canInviteUsers: boolean;
  readonly inviteForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    fullName: ['', [Validators.maxLength(120)]]
  });

  constructor(
    private readonly usersService: UsersService,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {
    this.canInviteUsers = this.authService.hasPermission('Users.Create');
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

  inviteUser(): void {
    if (!this.canInviteUsers || this.isInviting) {
      return;
    }

    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    const { email, fullName } = this.inviteForm.getRawValue();
    this.isInviting = true;
    this.inviteError = null;

    this.usersService
      .inviteUser({
        email: email.trim().toLowerCase(),
        fullName: fullName.trim() || null
      })
      .pipe(finalize(() => (this.isInviting = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Invitation sent. The user can now set a password from the email link.', 'Users');
          this.inviteForm.reset({ email: '', fullName: '' });
          this.loadUsers();
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.inviteError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  get emailControl() {
    return this.inviteForm.controls.email;
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
