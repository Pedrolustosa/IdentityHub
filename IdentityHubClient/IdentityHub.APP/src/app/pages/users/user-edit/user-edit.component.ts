import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { RoleListItem, RolesService } from '../../../services/roles.service';
import { UserListItem, UsersService } from '../../../services/users.service';

function sortedRoles(roles: string[]): string {
  return [...roles].sort().join('|');
}

@Component({
  selector: 'app-user-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './user-edit.component.html',
  styleUrl: './user-edit.component.css'
})
export class UserEditComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = true;
  loadError = false;
  isSaving = false;
  userId = '';
  user: UserListItem | null = null;
  availableRoles: RoleListItem[] = [];
  rolesLoadFailed = false;
  selectedRoleNames: string[] = [];
  private initialRoleNames: string[] = [];

  readonly editForm = this.formBuilder.nonNullable.group({
    fullName: [''],
    isActive: [true]
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly usersService: UsersService,
    private readonly rolesService: RolesService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/users']);
      return;
    }
    this.userId = id;

    forkJoin({
      user: this.usersService.getUserById(id),
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
          this.availableRoles = roles.filter((r) => r.name);
          this.initialRoleNames = [...(user.roles ?? [])];
          this.selectedRoleNames = [...this.initialRoleNames];
          this.editForm.patchValue({
            fullName: user.fullName ?? '',
            isActive: user.isActive
          });
        },
        error: () => {
          this.loadError = true;
          this.toastr.error('Could not load user for editing.', 'Users');
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

  private rolesChanged(): boolean {
    return sortedRoles(this.selectedRoleNames) !== sortedRoles(this.initialRoleNames);
  }

  cancel(): void {
    void this.router.navigate(['/app/users', this.userId]);
  }

  submit(): void {
    if (!this.userId || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const { fullName, isActive } = this.editForm.getRawValue();
    this.isSaving = true;

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

          if (this.rolesChanged() && !this.rolesLoadFailed && this.availableRoles.length > 0) {
            this.usersService.updateUserRoles(this.userId, { roles: this.selectedRoleNames }).subscribe({
              next: () => {
                this.toastr.success('User and roles updated.', 'Users');
                void this.router.navigate(['/app/users', this.userId]);
                finish();
              },
              error: () => {
                this.toastr.warning('Profile saved, but roles could not be updated.', 'Users');
                void this.router.navigate(['/app/users', this.userId]);
                finish();
              }
            });
          } else {
            if (this.rolesChanged() && (this.rolesLoadFailed || this.availableRoles.length === 0)) {
              this.toastr.warning('Profile saved. Roles were not changed (roles list unavailable).', 'Users');
            } else {
              this.toastr.success('User updated.', 'Users');
            }
            void this.router.navigate(['/app/users', this.userId]);
            finish();
          }
        },
        error: () => {
          this.isSaving = false;
          this.toastr.error('Could not update user.', 'Users');
        }
      });
  }
}
