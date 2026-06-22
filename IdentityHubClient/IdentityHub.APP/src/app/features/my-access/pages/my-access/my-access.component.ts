import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import {
  AuthService,
  MeResponse,
  UserSessionResponse
} from '../../../../core/services/auth.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { ACCESS_SCREEN_CATALOG } from '../../../../shared/constants/navigation-catalog';

@Component({
  selector: 'app-my-access',
  standalone: true,
  imports: [CommonModule, UxStateComponent],
  templateUrl: './my-access.component.html',
  styleUrl: './my-access.component.css'
})
export class MyAccessComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  me: MeResponse | null = null;
  currentSession: UserSessionResponse | null = null;
  lastPermissionsUpdateAt: string | null = null;

  readonly routeCatalog = ACCESS_SCREEN_CATALOG;

  constructor(
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    forkJoin({
      me: this.authService.getMe(),
      sessions: this.authService.getSessions()
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: ({ me, sessions }) => {
          this.me = me;
          this.currentSession = sessions.find((session) => session.isCurrent) ?? null;
          this.lastPermissionsUpdateAt = new Date().toISOString();
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'My Access');
        }
      });
  }

  isRouteAllowed(requiredAny: string[]): boolean {
    if (!this.me) {
      return false;
    }

    if (requiredAny.length === 0) {
      return true;
    }

    return requiredAny.some((perm) => this.me!.permissions.includes(perm));
  }

  allowedRoutes(): Array<{ label: string; route: string }> {
    return this.routeCatalog
      .filter((entry) => this.isRouteAllowed(entry.requiredAny))
      .map((entry) => ({ label: entry.label, route: entry.route }));
  }

  blockedRoutes(): Array<{ label: string; requiredAny: string[] }> {
    return this.routeCatalog
      .filter((entry) => !this.isRouteAllowed(entry.requiredAny))
      .map((entry) => ({ label: entry.label, requiredAny: entry.requiredAny }));
  }
}
