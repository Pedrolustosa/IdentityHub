import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CriticalActionConfirmationService {
  private confirm(message: string): boolean {
    if (typeof window === 'undefined') {
      return true;
    }

    return window.confirm(message);
  }

  confirmDeleteRole(roleName: string): boolean {
    return this.confirm(`Delete role "${roleName}"? This cannot be undone.`);
  }

  confirmUpdateUserRoles(userLabel: string): boolean {
    return this.confirm(
      `Update roles for ${userLabel}? This can change access immediately and may require a session refresh.`
    );
  }

  confirmRevokeUserSessions(userLabel: string): boolean {
    return this.confirm(`Revoke all active sessions for ${userLabel}?`);
  }

  confirmRevokeCurrentSession(): boolean {
    return this.confirm('Sign out this current session now?');
  }

  confirmRevokeSession(): boolean {
    return this.confirm('Revoke this session now?');
  }

  confirmRevokeOtherSessions(): boolean {
    return this.confirm('Revoke all other active sessions for your account?');
  }

  confirmBulkInactivateUsers(count: number): boolean {
    const subject = count === 1 ? 'this user' : `${count} selected users`;
    return this.confirm(`Inactivate ${subject}? This changes access immediately.`);
  }

  confirmResolveCriticalAlert(alertLabel: string): boolean {
    return this.confirm(`Resolve critical alert "${alertLabel}"? This should be done only after verification.`);
  }
}
