import { afterNextRender, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, RouterLink, BrandLogoComponent, UxStateComponent],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.css'
})
export class ConfirmEmailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly toastr = inject(ToastrService);

  isLoading = true;
  status: 'success' | 'error' | 'invalid' | null = null;
  message = '';
  loadError: UiLoadError | null = null;
  private confirmEmail = '';
  private confirmToken = '';

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

        this.confirmEmail = email.trim();
        this.confirmToken = token.trim();
        this.attemptConfirm();
      });
    });
  }

  attemptConfirm(): void {
    this.isLoading = true;
    this.status = null;
    this.loadError = null;
    this.message = '';

    this.authService.confirmEmail(this.confirmEmail, this.confirmToken).subscribe({
      next: (body) => {
        this.isLoading = false;
        this.status = 'success';
        this.message = body?.trim() || 'Email confirmed successfully.';
      },
      error: (err: unknown) => {
        this.isLoading = false;
        this.status = 'error';
        const mapped = mapHttpToUiLoadError(err);
        this.loadError = mapped;
        this.toastr.error(toastMessageForUiLoadError(mapped), 'Email Confirmation');
      }
    });
  }
}
