import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { TokenService } from '../../../core/auth/session/token.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form class="form" [formGroup]="form" (ngSubmit)="onSubmit()" novalidate>
      <h1>Sign in</h1>
      <p class="lead">Administrator access to ReelBox operations</p>

      @if (errorMessage()) {
        <div class="error" role="alert">{{ errorMessage() }}</div>
      }

      <label>
        <span>Email</span>
        <input
          type="email"
          formControlName="email"
          autocomplete="username"
          placeholder="admin@example.com"
        />
        @if (showError('email')) {
          <small>Enter a valid email.</small>
        }
      </label>

      <label>
        <span>Password</span>
        <div class="password-row">
          <input
            [type]="showPassword() ? 'text' : 'password'"
            formControlName="password"
            autocomplete="current-password"
            placeholder="••••••••"
          />
          <button type="button" class="toggle" (click)="showPassword.set(!showPassword())">
            {{ showPassword() ? 'Hide' : 'Show' }}
          </button>
        </div>
        @if (showError('password')) {
          <small>Password must be at least 8 characters.</small>
        }
      </label>

      <label class="remember">
        <input type="checkbox" formControlName="rememberMe" />
        <span>Remember me</span>
      </label>

      <button type="submit" class="cta" [disabled]="loading() || form.invalid">
        {{ loading() ? 'Signing in…' : 'Sign in' }}
      </button>

      <p class="note">
        Uses Admin Auth endpoints only (<code>/api/admin/auth/*</code>). Mobile auth is unchanged.
      </p>
    </form>
  `,
  styles: [
    `
      h1 {
        margin: 0 0 0.35rem;
        font-size: 1.45rem;
        letter-spacing: -0.02em;
      }
      .lead {
        margin: 0 0 1.25rem;
        color: var(--text-muted);
        font-size: 0.9rem;
      }
      .error {
        margin-bottom: 1rem;
        padding: 0.75rem 0.85rem;
        border-radius: 12px;
        border: 1px solid color-mix(in srgb, var(--status-fail) 45%, transparent);
        background: color-mix(in srgb, var(--status-fail) 14%, transparent);
        color: var(--status-fail);
        font-size: 0.86rem;
        line-height: 1.4;
      }
      label {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        margin-bottom: 0.9rem;
        font-size: 0.82rem;
        color: var(--text-muted);
      }
      input[type='email'],
      input[type='password'],
      input[type='text'] {
        border: 1px solid color-mix(in srgb, var(--border) 75%, transparent);
        background: var(--surface-elevated);
        color: var(--text-primary);
        border-radius: 12px;
        padding: 0.75rem 0.9rem;
        outline: none;
        width: 100%;
      }
      input:focus {
        border-color: color-mix(in srgb, var(--brand) 55%, var(--border));
      }
      .password-row {
        display: flex;
        gap: 0.5rem;
      }
      .toggle {
        border: 1px solid color-mix(in srgb, var(--border) 75%, transparent);
        background: var(--surface-elevated);
        color: var(--text-primary);
        border-radius: 12px;
        padding: 0 0.85rem;
        cursor: pointer;
        white-space: nowrap;
      }
      small {
        color: var(--status-fail);
      }
      .remember {
        flex-direction: row;
        align-items: center;
        gap: 0.55rem;
        margin-bottom: 1rem;
      }
      .cta {
        width: 100%;
        border: none;
        border-radius: 999px;
        padding: 0.85rem 1rem;
        font-weight: 600;
        cursor: pointer;
        color: #fff;
        background: var(--cta-gradient);
      }
      .cta:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .note {
        margin: 1rem 0 0;
        font-size: 0.75rem;
        color: var(--text-muted);
        line-height: 1.4;
      }
      code {
        font-size: 0.72rem;
      }
    `,
  ],
})
export class LoginPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly tokens = inject(TokenService);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);
  readonly submitted = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    rememberMe: [this.tokens.isRememberMe()],
  });

  showError(controlName: 'email' | 'password'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.touched || this.submitted());
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.form.markAllAsTouched();

    if (this.form.invalid || this.loading()) {
      return;
    }

    const { email, password, rememberMe } = this.form.getRawValue();
    this.loading.set(true);

    this.auth.login({ email: email.trim(), password }, rememberMe).subscribe({
      next: () => {
        this.loading.set(false);
        void this.router.navigateByUrl('/dashboard');
      },
      error: (err: Error) => {
        this.loading.set(false);
        this.errorMessage.set(err.message || 'Sign in failed.');
      },
    });
  }
}
