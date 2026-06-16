import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../../core/services/auth.service';
import { PERMISSION_CATALOG } from '../../../../../shared/constants/permissions-catalog';
import { LoadErrorBannerComponent } from '../../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { RoleListItem, RolesService } from '../../../roles.service';

function uniqueSorted(values: string[]): string[] {
  return [...new Set(values.map((v) => v.trim()).filter(Boolean))].sort((a, b) =>
    a.localeCompare(b, undefined, { sensitivity: 'base' })
  );
}

@Component({
  selector: 'app-role-claims-edit',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './role-claims-edit.component.html',
  styleUrl: './role-claims-edit.component.css'
})
export class RoleClaimsEditComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  saveError: UiLoadError | null = null;
  isSaving = false;
  roleId = '';
  role: RoleListItem | null = null;
  /** Rows shown in the UI: fixed after load; never shrink when toggling checkboxes. */
  permissionRows: string[] = [];
  selectedPermissions: string[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly rolesService: RolesService,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {}

  get canAssignRolePermissions(): boolean {
    return this.authService.canAssignRolePermissions();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('roleId');
    if (!id) {
      void this.router.navigate(['/app/role-claims']);
      return;
    }
    this.roleId = id;
    this.loadRoleEditData();
  }

  loadRoleEditData(): void {
    this.isLoading = true;
    this.loadError = null;
    this.role = null;
    this.permissionRows = [];
    this.selectedPermissions = [];

    forkJoin({
      role: this.rolesService.getRoleById(this.roleId),
      assigned: this.rolesService.getRolePermissions(this.roleId),
      catalog: this.rolesService.getPermissionCatalog().pipe(catchError(() => of([...PERMISSION_CATALOG])))
    })
      .pipe(
        map(({ role, assigned, catalog }) => {
          const assignedList = assigned ?? [];
          return { role, assignedList, catalogList: catalog ?? [...PERMISSION_CATALOG] };
        }),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: ({ role, assignedList, catalogList }) => {
          this.role = role;
          const union = uniqueSorted([...catalogList, ...assignedList]);
          this.permissionRows = [...union];
          this.selectedPermissions = [...assignedList];
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Role Permissions');
        }
      });
  }

  isChecked(permission: string): boolean {
    return this.selectedPermissions.includes(permission);
  }

  togglePermission(permission: string, checked: boolean): void {
    if (!this.canAssignRolePermissions) {
      return;
    }
    if (checked) {
      if (!this.selectedPermissions.includes(permission)) {
        this.selectedPermissions = [...this.selectedPermissions, permission];
      }
    } else {
      this.selectedPermissions = this.selectedPermissions.filter((p) => p !== permission);
    }
  }

  cancel(): void {
    void this.router.navigate(['/app/role-claims', this.roleId]);
  }

  submit(): void {
    if (!this.roleId || this.isSaving || !this.canAssignRolePermissions) {
      return;
    }

    this.isSaving = true;
    this.saveError = null;
    this.rolesService.updateRolePermissions(this.roleId, this.selectedPermissions).subscribe({
      next: () => {
        this.toastr.success('Role permissions updated.', 'Role Permissions');
        this.isSaving = false;
      },
      error: (err: unknown) => {
        this.isSaving = false;
        const mapped = mapHttpToUiLoadError(err);
        this.saveError = mapped;
        this.toastr.error(toastMessageForUiLoadError(mapped), 'Role Permissions');
      }
    });
  }
}
