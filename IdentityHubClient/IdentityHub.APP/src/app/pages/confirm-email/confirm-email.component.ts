import { afterNextRender, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { take } from 'rxjs';
import { AuthService } from '../../services/auth.service';

function confirmEmailErrorMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) {
    return 'Could not confirm your email. Try again or request a new link.';
  }

  if (typeof err.error === 'string' && err.error.trim()) {
    return err.error;
  }

  if (typeof err.error === 'object' && err.error !== null) {
    const o = err.error as { detail?: string; title?: string; message?: string };
    const fromApi = o.detail ?? o.message ?? o.title;
    if (typeof fromApi === 'string' && fromApi.trim()) {
      return fromApi;
    }
  }

  return 'Could not confirm your email. The link may be invalid or expired.';
}

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.css'
})
export class ConfirmEmailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  isLoading = true;
  status: 'success' | 'error' | 'invalid' | null = null;
  message = '';

  constructor() {
    afterNextRender(() => {
      this.route.queryParams.pipe(take(1)).subscribe((params) => {
        const email = params['email'];
        const token = params['token'];

        if (typeof email !== 'string' || typeof token !== 'string' || !email.trim() || !token.trim()) {
          this.isLoading = false;
          this.status = 'invalid';
          return;
        }

        this.authService.confirmEmail(email.trim(), token.trim()).subscribe({
          next: (body) => {
            this.isLoading = false;
            this.status = 'success';
            this.message = body?.trim() || 'Email confirmed successfully.';
          },
          error: (err: unknown) => {
            this.isLoading = false;
            this.status = 'error';
            this.message = confirmEmailErrorMessage(err);
          }
        });
      });
    });
  }
}
