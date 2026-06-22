import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserInviteItem {
  id: string;
  email: string;
  fullName: string;
  role: string;
  status: 'Pending' | 'Accepted' | 'Expired' | 'Canceled';
  sentAt: string;
}

export interface UserInvitesPagedResponse {
  items: UserInviteItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class UserInvitesService {
  private readonly apiUrl = `${environment.apiUrl}/user-invites`;

  constructor(private readonly http: HttpClient) {}

  getUserInvites(page = 1, pageSize = 20): Observable<UserInvitesPagedResponse> {
    return this.http.get<UserInvitesPagedResponse>(this.apiUrl, {
      params: { page, pageSize }
    });
  }

  resendInvite(inviteId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/${inviteId}/resend`, {});
  }

  cancelInvite(inviteId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${inviteId}`);
  }
}
