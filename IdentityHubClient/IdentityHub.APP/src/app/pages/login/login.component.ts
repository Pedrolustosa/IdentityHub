import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, take } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { ToastrService } from 'ngx-toastr';

function loginErrorMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) {
    return 'Login failed. Please check your credentials.';
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

  return 'Login failed. Please check your credentials.';
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  emailNotConfirmed = false;

  readonly loginForm: FormGroup;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
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

    this.errorMessage = '';
    this.successMessage = '';
    this.emailNotConfirmed = false;
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
          const storage = formValue.rememberMe ? localStorage : sessionStorage;
          const otherStorage = formValue.rememberMe ? sessionStorage : localStorage;

          otherStorage.removeItem('accessToken');
          otherStorage.removeItem('refreshToken');
          storage.setItem('accessToken', response.token);
          storage.setItem('refreshToken', response.refreshToken);

          this.successMessage = 'Login successful.';
          this.toastr.success('You are now signed in.', 'Success');
          void this.router.navigate(['/app/dashboard']);
        },
        error: (error: unknown) => {
          const msg = loginErrorMessage(error);
          this.errorMessage = msg;
          this.emailNotConfirmed = msg.toLowerCase().includes('email not confirmed');
          this.toastr.error(msg, 'Login failed');
        }
      });
  }
}
