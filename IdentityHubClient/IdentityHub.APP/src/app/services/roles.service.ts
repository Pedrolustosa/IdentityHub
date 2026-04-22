import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RoleListItem {
  id: string;
  name: string | null;
}

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly rolesApiUrl = 'https://localhost:7039/api/roles';

  constructor(private readonly http: HttpClient) {}

  getRoles(): Observable<RoleListItem[]> {
    return this.http.get<RoleListItem[]>(this.rolesApiUrl);
  }

  getRoleById(id: string): Observable<RoleListItem> {
    return this.http.get<RoleListItem>(`${this.rolesApiUrl}/${encodeURIComponent(id)}`);
  }

  getRolePermissions(id: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.rolesApiUrl}/${encodeURIComponent(id)}/permissions`);
  }

  updateRolePermissions(roleId: string, permissions: string[]): Observable<string> {
    return this.http.put(`${this.rolesApiUrl}/${encodeURIComponent(roleId)}/permissions`, { permissions }, {
      responseType: 'text'
    });
  }
}
