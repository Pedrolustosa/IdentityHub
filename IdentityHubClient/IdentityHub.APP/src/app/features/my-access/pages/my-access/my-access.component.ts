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
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-my-access',
  standalone: true,
  imports: [CommonModule, LoadErrorBannerComponent],
  templateUrl: './my-access.component.html',
  styleUrl: './my-access.component.css'
})
export class MyAccessComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  me: MeResponse | null = null;
  currentSession: UserSessionResponse | null = null;

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
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'My Access');
        }
      });
  }
}
