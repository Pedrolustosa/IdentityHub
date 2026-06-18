import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserListItem {
  id: string;
  email: string | null;
  fullName: string | null;
  isActive: boolean;
  emailConfirmed?: boolean;
  lastLoginAt?: string | null;
  activeSessions?: number;
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
  isActive?: boolean;
  roles?: string[];
}

export interface UpdateUserRequest {
  fullName?: string | null;
  isActive: boolean;
}

export interface UpdateRolesRequest {
  roles: string[];
}

export interface UserSessionItem {
  id: string;
  ipAddress: string;
  browser: string;
  operatingSystem: string;
  createdAt: string;
  lastAccessAt: string | null;
  revokedAt?: string | null;
  isActive?: boolean;
  isCurrent: boolean;
}

export interface UserAuditItem {
  id: string;
  actorUserId: string;
  type: string;
  targetId?: string | null;
  description: string;
  metadataJson?: string | null;
  createdAt: string;
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

  getUserSessions(id: string): Observable<UserSessionItem[]> {
    return this.http.get<UserSessionItem[]>(`${this.usersApiUrl}/${encodeURIComponent(id)}/sessions`);
  }

  getUserSessionsHistory(id: string, take = 20): Observable<UserSessionItem[]> {
    const params = new HttpParams().set('take', take);
    return this.http.get<UserSessionItem[]>(`${this.usersApiUrl}/${encodeURIComponent(id)}/sessions/history`, { params });
  }

  getUserAudit(id: string, take = 20): Observable<UserAuditItem[]> {
    const params = new HttpParams().set('take', take);
    return this.http.get<UserAuditItem[]>(`${this.usersApiUrl}/${encodeURIComponent(id)}/audit`, { params });
  }
}
