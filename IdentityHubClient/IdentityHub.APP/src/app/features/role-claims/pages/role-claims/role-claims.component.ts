import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { CriticalActionConfirmationService } from '../../../../shared/services/critical-action-confirmation.service';
import { RoleListItem, RolesService } from '../../roles.service';

export interface RoleWithPermissionMeta {
  role: RoleListItem;
  permissionCount: number | null;
}

@Component({
  selector: 'app-role-claims',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, UxStateComponent],
  templateUrl: './role-claims.component.html',
  styleUrl: './role-claims.component.css'
})
export class RoleClaimsComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  rows: RoleWithPermissionMeta[] = [];
  searchTerm = '';
  showCreateModal = false;
  newRoleName = '';
  isCreating = false;
  deletingRoleId: string | null = null;
  readonly canAssignRolePermissions: boolean;
  readonly canCreateRole: boolean;
  readonly canDeleteRole: boolean;

  constructor(
    private readonly rolesService: RolesService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canAssignRolePermissions = this.authService.canAssignRolePermissions();
    this.canCreateRole = this.authService.hasPermission('Roles.Create');
    this.canDeleteRole = this.authService.hasPermission('Roles.Delete');
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    this.rolesService
      .getRoles()
      .pipe(
        switchMap((roles) => {
          if (!roles.length) {
            return of<RoleWithPermissionMeta[]>([]);
          }
          return forkJoin(
            roles.map((role) =>
              this.rolesService.getRolePermissions(role.id).pipe(
                map((permissions) => ({
                  role,
                  permissionCount: permissions.length
                })),
                catchError(() => of<RoleWithPermissionMeta>({ role, permissionCount: null }))
              )
            )
          );
        }),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: (rows) => {
          this.rows = rows;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Role Permissions');
        }
      });
  }

  rolesWithPermissionsLoadedCount(): number {
    return this.rows.filter((row) => row.permissionCount !== null).length;
  }

  rolesWithoutPermissionsLoadedCount(): number {
    return this.rows.filter((row) => row.permissionCount === null).length;
  }

  filteredRows(): RoleWithPermissionMeta[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.rows;
    return this.rows.filter((row) => (row.role.name ?? '').toLowerCase().includes(term));
  }

  createRole(): void {
    const name = this.newRoleName.trim();
    if (!name || this.isCreating) return;

    this.isCreating = true;
    this.rolesService
      .createRole({ name })
      .pipe(finalize(() => (this.isCreating = false)))
      .subscribe({
        next: () => {
          this.toastr.success(`Role "${name}" created.`, 'Roles');
          this.showCreateModal = false;
          this.newRoleName = '';
          this.load();
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Roles');
        }
      });
  }

  deleteRole(row: RoleWithPermissionMeta): void {
    if (this.deletingRoleId || !row.role.name) return;

    if (!this.criticalActionConfirmationService.confirmDeleteRole(row.role.name)) return;

    this.deletingRoleId = row.role.id;
    this.rolesService
      .deleteRole(row.role.id)
      .pipe(finalize(() => (this.deletingRoleId = null)))
      .subscribe({
        next: () => {
          this.rows = this.rows.filter((r) => r.role.id !== row.role.id);
          this.toastr.success(`Role "${row.role.name}" deleted.`, 'Roles');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Roles');
        }
      });
  }
}
