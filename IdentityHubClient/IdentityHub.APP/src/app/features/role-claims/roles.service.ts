import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RoleListItem {
  id: string;
  name: string | null;
  userCount?: number;
}

export interface CreateRoleRequest {
  name: string;
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly rolesApiUrl = `${environment.apiUrl}/roles`;

  constructor(private readonly http: HttpClient) {}

  getRoles(): Observable<RoleListItem[]> {
    return this.http.get<RoleListItem[]>(this.rolesApiUrl);
  }

  getRoleById(id: string): Observable<RoleListItem> {
    return this.http.get<RoleListItem>(`${this.rolesApiUrl}/${encodeURIComponent(id)}`);
  }

  getPermissionCatalog(): Observable<string[]> {
    return this.http.get<string[]>(`${this.rolesApiUrl}/permissions/catalog`);
  }

  getRolePermissions(id: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.rolesApiUrl}/${encodeURIComponent(id)}/permissions`);
  }

  updateRolePermissions(roleId: string, permissions: string[]): Observable<string> {
    return this.http.put(`${this.rolesApiUrl}/${encodeURIComponent(roleId)}/permissions`, { permissions }, {
      responseType: 'text'
    });
  }

  createRole(body: CreateRoleRequest): Observable<string> {
    return this.http.post(`${this.rolesApiUrl}`, body, { responseType: 'text' });
  }

  deleteRole(id: string): Observable<string> {
    return this.http.delete(`${this.rolesApiUrl}/${encodeURIComponent(id)}`, { responseType: 'text' });
  }
}
