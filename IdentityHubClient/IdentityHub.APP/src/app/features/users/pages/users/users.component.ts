import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { CriticalActionConfirmationService } from '../../../../shared/services/critical-action-confirmation.service';
import { UserListItem, UsersService } from '../../users.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, UxStateComponent],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  isLoading = true;
  isBulkProcessing = false;
  loadError: UiLoadError | null = null;
  users: UserListItem[] = [];
  selectedUserIds = new Set<string>();
  readonly statusOptions = ['all', 'active', 'inactive'] as const;
  statusFilter: 'all' | 'active' | 'inactive' = 'all';
  roleFilter = '';
  searchTerm = '';
  sortBy: 'fullName' | 'email' | 'status' = 'fullName';
  sortDir: 'asc' | 'desc' = 'asc';
  page = 1;
  readonly pageSize = 12;
  readonly canCreateUsers: boolean;
  readonly canEditUsers: boolean;
  readonly canUpdateUsers: boolean;

  constructor(
    private readonly usersService: UsersService,
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {
    this.canCreateUsers = this.authService.hasPermission('Users.Create');
    this.canEditUsers = this.authService.hasPermission('Users.Update');
    this.canUpdateUsers = this.authService.hasPermission('Users.Update');
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
          this.selectedUserIds.clear();
          this.page = 1;
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

  rolesCatalog(): string[] {
    return [...new Set(this.users.flatMap((user) => user.roles ?? []))].sort((a, b) => a.localeCompare(b));
  }

  filteredUsers(): UserListItem[] {
    const term = this.searchTerm.trim().toLowerCase();
    const roleFilter = this.roleFilter.trim().toLowerCase();

    const filtered = this.users.filter((user) => {
      const matchesStatus =
        this.statusFilter === 'all' ||
        (this.statusFilter === 'active' && user.isActive) ||
        (this.statusFilter === 'inactive' && !user.isActive);

      const matchesRole = !roleFilter || (user.roles ?? []).some((role) => role.toLowerCase() === roleFilter);

      const email = (user.email ?? '').toLowerCase();
      const fullName = (user.fullName ?? '').toLowerCase();
      const matchesSearch = !term || email.includes(term) || fullName.includes(term);

      return matchesStatus && matchesRole && matchesSearch;
    });

    return filtered.sort((a, b) => this.compareUsers(a, b));
  }

  pagedUsers(): UserListItem[] {
    const pages = this.totalPages();
    const safePage = Math.min(Math.max(this.page, 1), pages);
    if (safePage !== this.page) {
      this.page = safePage;
    }

    const start = (safePage - 1) * this.pageSize;
    return this.filteredUsers().slice(start, start + this.pageSize);
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredUsers().length / this.pageSize));
  }

  compareUsers(a: UserListItem, b: UserListItem): number {
    let result = 0;

    if (this.sortBy === 'status') {
      result = Number(a.isActive) - Number(b.isActive);
    } else if (this.sortBy === 'email') {
      result = (a.email ?? '').localeCompare(b.email ?? '', undefined, { sensitivity: 'base' });
    } else {
      result = (a.fullName ?? '').localeCompare(b.fullName ?? '', undefined, { sensitivity: 'base' });
    }

    return this.sortDir === 'asc' ? result : -result;
  }

  setSort(sortBy: 'fullName' | 'email' | 'status'): void {
    if (this.sortBy === sortBy) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
      return;
    }

    this.sortBy = sortBy;
    this.sortDir = 'asc';
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = 'all';
    this.roleFilter = '';
    this.page = 1;
  }

  onFilterChange(): void {
    this.page = 1;
  }

  hasActiveFilters(): boolean {
    return (
      this.searchTerm.trim().length > 0 ||
      this.statusFilter !== 'all' ||
      this.roleFilter.trim().length > 0
    );
  }

  activeFilterCount(): number {
    let count = 0;
    if (this.searchTerm.trim().length > 0) {
      count++;
    }
    if (this.statusFilter !== 'all') {
      count++;
    }
    if (this.roleFilter.trim().length > 0) {
      count++;
    }

    return count;
  }

  filteredCount(): number {
    return this.filteredUsers().length;
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page -= 1;
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages()) {
      this.page += 1;
    }
  }

  toggleSelection(userId: string, checked: boolean): void {
    if (checked) {
      this.selectedUserIds.add(userId);
      return;
    }

    this.selectedUserIds.delete(userId);
  }

  isSelected(userId: string): boolean {
    return this.selectedUserIds.has(userId);
  }

  toggleSelectPage(checked: boolean): void {
    for (const user of this.pagedUsers()) {
      if (checked) {
        this.selectedUserIds.add(user.id);
      } else {
        this.selectedUserIds.delete(user.id);
      }
    }
  }

  areAllPageSelected(): boolean {
    const pageUsers = this.pagedUsers();
    return pageUsers.length > 0 && pageUsers.every((user) => this.selectedUserIds.has(user.id));
  }

  selectedCount(): number {
    return this.selectedUserIds.size;
  }

  runBulkActivation(isActive: boolean): void {
    if (!this.canUpdateUsers || this.isBulkProcessing || this.selectedUserIds.size === 0) {
      return;
    }

    if (!isActive && !this.criticalActionConfirmationService.confirmBulkInactivateUsers(this.selectedUserIds.size)) {
      return;
    }

    this.isBulkProcessing = true;

    const updates = [...this.selectedUserIds].map((id) => {
      const user = this.users.find((entry) => entry.id === id);
      return this.usersService
        .updateUser(id, {
          fullName: user?.fullName ?? null,
          isActive
        })
        .pipe(
          map(() => ({ id, success: true })),
          catchError(() => of({ id, success: false }))
        );
    });

    forkJoin(updates)
      .pipe(finalize(() => (this.isBulkProcessing = false)))
      .subscribe({
        next: (results) => {
          const successIds = new Set(results.filter((result) => result.success).map((result) => result.id));
          const failedIds = new Set(results.filter((result) => !result.success).map((result) => result.id));
          const failedCount = results.length - successIds.size;

          this.users = this.users.map((user) =>
            successIds.has(user.id)
              ? {
                  ...user,
                  isActive
                }
              : user
          );

          if (successIds.size > 0) {
            this.toastr.success(
              `${successIds.size} user(s) marked as ${isActive ? 'active' : 'inactive'}.`,
              'Users'
            );
          }

          if (failedCount > 0) {
            this.toastr.warning(
              `${failedCount} user(s) could not be updated.`,
              'Users'
            );
          }

          this.selectedUserIds = failedIds;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }

  exportCsv(): void {
    const rows = this.filteredUsers();
    const csv = [
      ['Name', 'Email', 'Status', 'Roles', 'Email confirmed', 'Last login', 'Active sessions'].join(','),
      ...rows.map((user) =>
        [
          this.escapeCsv(user.fullName || ''),
          this.escapeCsv(user.email || ''),
          user.isActive ? 'Active' : 'Inactive',
          this.escapeCsv((user.roles ?? []).join('|')),
          user.emailConfirmed ? 'Yes' : 'No',
          this.escapeCsv(user.lastLoginAt ?? ''),
          String(user.activeSessions ?? 0)
        ].join(',')
      )
    ].join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `users-${new Date().toISOString().replace(/[:.]/g, '-')}.csv`;
    link.click();
    URL.revokeObjectURL(url);
  }

  private escapeCsv(value: string): string {
    const normalized = value.replace(/"/g, '""');
    return `"${normalized}"`;
  }
}
