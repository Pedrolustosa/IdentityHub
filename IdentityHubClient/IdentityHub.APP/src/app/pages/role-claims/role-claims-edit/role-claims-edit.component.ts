import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize, map } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { PERMISSION_CATALOG } from '../../../constants/permissions-catalog';
import { RoleListItem, RolesService } from '../../../services/roles.service';

function uniqueSorted(values: string[]): string[] {
  return [...new Set(values.map((v) => v.trim()).filter(Boolean))].sort((a, b) =>
    a.localeCompare(b, undefined, { sensitivity: 'base' })
  );
}

@Component({
  selector: 'app-role-claims-edit',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './role-claims-edit.component.html',
  styleUrl: './role-claims-edit.component.css'
})
export class RoleClaimsEditComponent implements OnInit {
  isLoading = true;
  loadError = false;
  isSaving = false;
  roleId = '';
  role: RoleListItem | null = null;
  permissionCatalog: string[] = [];
  selectedPermissions: string[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly rolesService: RolesService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('roleId');
    if (!id) {
      void this.router.navigate(['/app/role-claims']);
      return;
    }
    this.roleId = id;

    forkJoin({
      role: this.rolesService.getRoleById(id),
      assigned: this.rolesService.getRolePermissions(id)
    })
      .pipe(
        map(({ role, assigned }) => {
          const assignedList = assigned ?? [];
          const merged = uniqueSorted([...PERMISSION_CATALOG, ...assignedList]);
          return { role, assignedList, merged };
        }),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: ({ role, assignedList, merged }) => {
          this.role = role;
          this.permissionCatalog = merged.length > 0 ? merged : assignedList;
          this.selectedPermissions = [...assignedList];
        },
        error: () => {
          this.loadError = true;
          this.toastr.error('Could not load role for editing.', 'Role claims');
        }
      });
  }

  isChecked(permission: string): boolean {
    return this.selectedPermissions.includes(permission);
  }

  togglePermission(permission: string, checked: boolean): void {
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
    if (!this.roleId || this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.rolesService.updateRolePermissions(this.roleId, this.selectedPermissions).subscribe({
      next: () => {
        this.toastr.success('Permissions updated.', 'Role claims');
        this.isSaving = false;
        void this.router.navigate(['/app/role-claims', this.roleId]);
      },
      error: () => {
        this.isSaving = false;
        this.toastr.error('Could not update permissions. You need Roles.Manage.', 'Role claims');
      }
    });
  }
}
