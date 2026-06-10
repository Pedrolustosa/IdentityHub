import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserListItem {
  id: string;
  email: string | null;
  fullName: string | null;
  isActive: boolean;
  roles?: string[] | null;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  fullName?: string | null;
}

export interface InviteUserRequest {
  email: string;
  fullName?: string | null;
}

export interface UpdateUserRequest {
  fullName?: string | null;
  isActive: boolean;
}

export interface UpdateRolesRequest {
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly usersApiUrl = `${environment.apiUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  getUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(this.usersApiUrl);
  }

  getUserById(id: string): Observable<UserListItem> {
    return this.http.get<UserListItem>(`${this.usersApiUrl}/${encodeURIComponent(id)}`);
  }

  createUser(body: CreateUserRequest): Observable<string> {
    return this.http.post(`${this.usersApiUrl}`, body, { responseType: 'text' });
  }

  inviteUser(body: InviteUserRequest): Observable<string> {
    return this.http.post(`${this.usersApiUrl}/invite`, body, { responseType: 'text' });
  }

  updateUser(id: string, body: UpdateUserRequest): Observable<string> {
    return this.http.put(`${this.usersApiUrl}/${encodeURIComponent(id)}`, body, {
      responseType: 'text'
    });
  }

  updateUserRoles(id: string, body: UpdateRolesRequest): Observable<string> {
    return this.http.put(`${this.usersApiUrl}/${encodeURIComponent(id)}/roles`, body, {
      responseType: 'text'
    });
  }
}
