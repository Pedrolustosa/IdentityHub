import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { UserListItem, UsersService } from '../../services/users.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  isLoading = true;
  hasError = false;
  users: UserListItem[] = [];

  constructor(
    private readonly usersService: UsersService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.isLoading = true;
    this.hasError = false;

    this.usersService
      .getUsers()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (users) => {
          this.users = users;
        },
        error: () => {
          this.hasError = true;
          this.toastr.error('Failed to load users list.', 'Users');
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
}
