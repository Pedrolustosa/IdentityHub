import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class RoleClaimsService {
  private readonly roleClaimsApiUrl = 'https://localhost:7039/api/RoleClaims';

  constructor(private readonly http: HttpClient) {}

  getAllPermissions(): Observable<string[]> {
    return this.http.get<string[]>(this.roleClaimsApiUrl);
  }

  getPermissionsByRole(roleId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.roleClaimsApiUrl}/${encodeURIComponent(roleId)}`);
  }

  updateRolePermissions(roleId: string, permissions: string[]): Observable<string> {
    return this.http.put(`${this.roleClaimsApiUrl}/${encodeURIComponent(roleId)}`, permissions, {
      responseType: 'text'
    });
  }
}
