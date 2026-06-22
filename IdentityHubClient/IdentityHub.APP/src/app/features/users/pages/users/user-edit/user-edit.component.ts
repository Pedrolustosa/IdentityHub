import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../../core/services/auth.service';
import { UxStateComponent } from '../../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { CriticalActionConfirmationService } from '../../../../../shared/services/critical-action-confirmation.service';
import { RoleListItem, RolesService } from '../../../../role-claims/roles.service';
import { UserListItem, UsersService } from '../../../users.service';

function sortedRoles(roles: string[]): string {
  return [...roles].sort().join('|');
}

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, UxStateComponent],
  templateUrl: './user-edit.component.html',
  styleUrl: './user-edit.component.css'
})
export class UserEditComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = true;
  loadError: UiLoadError | null = null;
  isSaving = false;
  saveError: UiLoadError | null = null;
  showConfirm = false;
  userId = '';
  user: UserListItem | null = null;
  isEditingSelf = false;
  availableRoles: RoleListItem[] = [];
  rolesLoadFailed = false;
  selectedRoleNames: string[] = [];
  initialRoleNames: string[] = [];
  initialFullName = '';
  initialIsActive = true;
  readonly canUpdateUserRoles: boolean;
  readonly criticalRoles = ['Admin', 'Administrator', 'SuperAdmin'];
  readonly criticalPermissions = ['Users.Delete', 'Roles.Permissions.Update', 'Audit.View', 'SecurityEvents.Manage'];

  readonly editForm = this.formBuilder.nonNullable.group({
    fullName: [''],
    isActive: [true]
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly usersService: UsersService,
    private readonly rolesService: RolesService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canUpdateUserRoles = this.authService.hasPermission('Users.Roles.Update');
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/users']);
      return;
    }
    this.userId = id;
    this.loadUserData();
  }

  loadUserData(): void {
    this.isLoading = true;
    this.loadError = null;
    this.user = null;
    this.rolesLoadFailed = false;

    forkJoin({
      user: this.usersService.getUserById(this.userId),
      roles: this.rolesService.getRoles().pipe(
        catchError(() => {
          this.rolesLoadFailed = true;
          return of<RoleListItem[]>([]);
        })
      )
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: ({ user, roles }) => {
          this.user = user;
          const currentId = this.authService.getCurrentUserId();
          this.isEditingSelf = !!currentId && currentId === user.id;
          this.availableRoles = roles.filter((r) => r.name);
          this.initialRoleNames = [...(user.roles ?? [])];
          this.selectedRoleNames = [...this.initialRoleNames];
          this.initialFullName = user.fullName ?? '';
          this.initialIsActive = user.isActive;
          this.editForm.patchValue({
            fullName: user.fullName ?? '',
            isActive: user.isActive
          });
          if (this.isEditingSelf) {
            this.editForm.controls['isActive'].disable({ emitEvent: false });
          } else {
            this.editForm.controls['isActive'].enable({ emitEvent: false });
          }
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  toggleRole(name: string, checked: boolean): void {
    if (checked) {
      if (!this.selectedRoleNames.includes(name)) {
        this.selectedRoleNames = [...this.selectedRoleNames, name];
      }
    } else {
      this.selectedRoleNames = this.selectedRoleNames.filter((r) => r !== name);
    }
  }

  isRoleChecked(name: string): boolean {
    return this.selectedRoleNames.includes(name);
  }

  private rolesChangedStr(): boolean {
    return sortedRoles(this.selectedRoleNames) !== sortedRoles(this.initialRoleNames);
  }

  rolesChanged(): boolean {
    return this.rolesChangedStr();
  }

  private promptRevokeSessionsAfterRoleChange(): void {
    if (!this.authService.hasPermission('Sessions.Revoke')) {
      void this.router.navigate(['/app/users', this.userId]);
      return;
    }

    const userLabel = this.user?.email ?? 'this user';
    const revoke = this.criticalActionConfirmationService.confirmUpdateUserRoles(userLabel);
    if (revoke) {
      this.authService
        .revokeUserSessions(this.userId)
        .subscribe({
          next: () => this.toastr.success('Sessions revoked.', 'Users'),
          error: () => this.toastr.warning('Could not revoke sessions.', 'Users')
        });
    }
    void this.router.navigate(['/app/users', this.userId]);
  }

  cancel(): void {
    void this.router.navigate(['/app/users', this.userId]);
  }

  rolesAdded(): string[] {
    return this.selectedRoleNames.filter((r) => !this.initialRoleNames.includes(r));
  }

  rolesRemoved(): string[] {
    return this.initialRoleNames.filter((r) => !this.selectedRoleNames.includes(r));
  }

  isRemovingCriticalRole(): boolean {
    return this.rolesRemoved().some((r) => this.criticalRoles.some((c) => r.toLowerCase().includes(c.toLowerCase())));
  }

  hasChanges(): boolean {
    const { fullName, isActive } = this.editForm.getRawValue();
    return (
      (fullName.trim() || '') !== this.initialFullName ||
      isActive !== this.initialIsActive ||
      this.rolesChanged()
    );
  }

  requestSave(): void {
    if (!this.userId || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.showConfirm = true;
  }

  cancelConfirm(): void {
    this.showConfirm = false;
  }

  submit(): void {
    if (!this.userId || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const { fullName, isActive } = this.editForm.getRawValue();
    this.isSaving = true;
    this.saveError = null;

    this.usersService
      .updateUser(this.userId, {
        fullName: fullName.trim() || null,
        isActive
      })
      .subscribe({
        next: () => {
          const finish = (): void => {
            this.isSaving = false;
          };

          if (this.rolesChanged() && this.canUpdateUserRoles && !this.rolesLoadFailed && this.availableRoles.length > 0) {
            this.usersService.updateUserRoles(this.userId, { roles: this.selectedRoleNames }).subscribe({
              next: () => {
                this.showConfirm = false;
                this.toastr.success('User and roles updated.', 'Users');
                this.promptRevokeSessionsAfterRoleChange();
                finish();
              },
              error: () => {
                this.toastr.warning('User profile updated, but roles could not be updated.', 'Users');
                void this.router.navigate(['/app/users', this.userId]);
                finish();
              }
            });
          } else {
            if (this.rolesChanged() && !this.canUpdateUserRoles) {
              this.toastr.warning('User profile updated. You do not have permission to update user roles.', 'Users');
            } else if (this.rolesChanged() && (this.rolesLoadFailed || this.availableRoles.length === 0)) {
              this.toastr.warning('User profile updated. Roles were not changed because the roles list is unavailable.', 'Users');
            } else {
              this.toastr.success('User updated.', 'Users');
            }
            this.showConfirm = false;
            void this.router.navigate(['/app/users', this.userId]);
            finish();
          }
        },
        error: (err: unknown) => {
          this.isSaving = false;
          const mapped = mapHttpToUiLoadError(err);
          this.saveError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }
}
