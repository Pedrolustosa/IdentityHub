import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { SessionTokensService } from '../../../../core/services/session-tokens.service';
import { ToastrService } from 'ngx-toastr';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  isLoading = false;
  successMessage = '';

  readonly loginForm: FormGroup;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly sessionTokens: SessionTokensService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService
  ) {
    this.loginForm = this.formBuilder.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [true]
    });
  }

  ngOnInit(): void {
    this.route.queryParams.pipe(take(1)).subscribe((params) => {
      const email = params['email'];
      if (typeof email === 'string' && email.trim()) {
        this.loginForm.patchValue({ email: email.trim() });
      }
    });
  }

  get emailControl() {
    return this.loginForm.get('email');
  }

  get passwordControl() {
    return this.loginForm.get('password');
  }

  submit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.successMessage = '';
    this.isLoading = true;

    const formValue = this.loginForm.getRawValue();
    this.authService
      .login({
        email: formValue.email.trim().toLowerCase(),
        password: formValue.password
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.sessionTokens.saveTokens(response.token, response.refreshToken, formValue.rememberMe);

          this.successMessage = 'Login successful.';
          this.toastr.success('You are now signed in.', 'Success');
          void this.router.navigate(['/app/dashboard']);
        },
        error: () => {
          this.toastr.warning(
            'Nao foi possivel entrar. Verifique seu e-mail e senha e tente novamente.',
            'Falha no acesso'
          );
        }
      });
  }
}
