import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

type ResetStep = 1 | 2 | 3;

@Component({
  selector: 'app-forgot-password-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="wrap">
      <h1>Reset password</h1>
      <p class="lead">Recover access to your ReelBox admin account</p>

      <ol class="steps" aria-label="Reset steps">
        <li [class.active]="step() === 1" [class.done]="step() > 1">Email</li>
        <li [class.active]="step() === 2" [class.done]="step() > 2">OTP</li>
        <li [class.active]="step() === 3">Password</li>
      </ol>

      @if (errorMessage()) {
        <div class="error" role="alert">{{ errorMessage() }}</div>
      }
      @if (infoMessage()) {
        <div class="info" role="status">{{ infoMessage() }}</div>
      }

      @if (step() === 1) {
        <form [formGroup]="emailForm" (ngSubmit)="submitEmail()" novalidate>
          <label>
            <span>Admin email</span>
            <input type="email" formControlName="email" autocomplete="username" placeholder="admin@example.com" />
            @if (emailForm.controls.email.invalid && (emailForm.controls.email.touched || submitted())) {
              <small>Enter a valid email.</small>
            }
          </label>
          <button type="submit" class="cta" [disabled]="loading() || emailForm.invalid">
            {{ loading() ? 'Sending…' : 'Send reset code' }}
          </button>
        </form>
      }

      @if (step() === 2) {
        <form [formGroup]="otpForm" (ngSubmit)="submitOtp()" novalidate>
          <label>
            <span>6-digit code</span>
            <input
              type="text"
              inputmode="numeric"
              maxlength="6"
              formControlName="otp"
              autocomplete="one-time-code"
              placeholder="••••••"
            />
            @if (otpForm.controls.otp.invalid && (otpForm.controls.otp.touched || submitted())) {
              <small>Enter the 6-digit code from your email.</small>
            }
          </label>
          <button type="submit" class="cta" [disabled]="loading() || otpForm.invalid">
            Continue
          </button>
          <button type="button" class="linkish" (click)="backToEmail()" [disabled]="loading()">
            Use a different email
          </button>
        </form>
      }

      @if (step() === 3) {
        <form [formGroup]="passwordForm" (ngSubmit)="submitPassword()" novalidate>
          <label>
            <span>New password</span>
            <input type="password" formControlName="password" autocomplete="new-password" placeholder="••••••••" />
            @if (passwordForm.controls.password.invalid && (passwordForm.controls.password.touched || submitted())) {
              <small>At least 8 characters.</small>
            }
          </label>
          <label>
            <span>Confirm password</span>
            <input type="password" formControlName="confirm" autocomplete="new-password" placeholder="••••••••" />
            @if (passwordMismatch() && (passwordForm.controls.confirm.touched || submitted())) {
              <small>Passwords do not match.</small>
            }
          </label>
          <button type="submit" class="cta" [disabled]="loading() || passwordForm.invalid || passwordMismatch()">
            {{ loading() ? 'Updating…' : 'Update password' }}
          </button>
        </form>
      }

      <p class="foot">
        <a routerLink="/auth/login">Back to sign in</a>
      </p>
    </div>
  `,
  styles: [
    `
      h1 {
        margin: 0 0 0.35rem;
        font-size: 1.45rem;
        letter-spacing: -0.02em;
      }
      .lead {
        margin: 0 0 1.1rem;
        color: var(--text-muted);
        font-size: 0.9rem;
      }
      .steps {
        list-style: none;
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 0.4rem;
        margin: 0 0 1.15rem;
        padding: 0;
      }
      .steps li {
        text-align: center;
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: var(--text-muted);
        border-bottom: 2px solid var(--border);
        padding-bottom: 0.45rem;
      }
      .steps li.active {
        color: var(--text-primary);
        border-bottom-color: var(--ig-pink, #dd2a7b);
        font-weight: 600;
      }
      .steps li.done {
        color: var(--status-ok);
        border-bottom-color: var(--status-ok);
      }
      .error,
      .info {
        margin-bottom: 1rem;
        padding: 0.75rem 0.85rem;
        border-radius: 12px;
        font-size: 0.86rem;
        line-height: 1.4;
      }
      .error {
        border: 1px solid color-mix(in srgb, var(--status-fail) 45%, transparent);
        background: color-mix(in srgb, var(--status-fail) 14%, transparent);
        color: var(--status-fail);
      }
      .info {
        border: 1px solid color-mix(in srgb, var(--status-ok) 40%, transparent);
        background: color-mix(in srgb, var(--status-ok) 12%, transparent);
        color: var(--text-primary);
      }
      label {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        margin-bottom: 0.9rem;
        font-size: 0.82rem;
        color: var(--text-muted);
      }
      input {
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
      small {
        color: var(--status-fail);
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
      .linkish {
        display: block;
        width: 100%;
        margin-top: 0.75rem;
        border: none;
        background: transparent;
        color: var(--ig-pink, #dd2a7b);
        cursor: pointer;
        font-size: 0.85rem;
      }
      .foot {
        margin: 1.15rem 0 0;
        text-align: center;
        font-size: 0.85rem;
      }
      .foot a {
        color: var(--ig-pink, #dd2a7b);
      }
    `,
  ],
})
export class ForgotPasswordPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly step = signal<ResetStep>(1);
  readonly loading = signal(false);
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly infoMessage = signal<string | null>(null);

  private emailValue = '';
  private otpValue = '';

  readonly emailForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly otpForm = this.fb.nonNullable.group({
    otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  readonly passwordForm = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required, Validators.minLength(8)]],
  });

  passwordMismatch(): boolean {
    const { password, confirm } = this.passwordForm.getRawValue();
    return confirm.length > 0 && password !== confirm;
  }

  backToEmail(): void {
    this.step.set(1);
    this.otpForm.reset();
    this.passwordForm.reset();
    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.submitted.set(false);
  }

  submitEmail(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.emailForm.markAllAsTouched();
    if (this.emailForm.invalid || this.loading()) {
      return;
    }

    const email = this.emailForm.controls.email.value.trim();
    this.loading.set(true);
    this.auth.forgotPassword({ email }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.emailValue = email;
        this.infoMessage.set(res.message);
        this.submitted.set(false);
        this.step.set(2);
      },
      error: (err: Error) => {
        this.loading.set(false);
        this.errorMessage.set(err.message || 'Could not send reset code.');
      },
    });
  }

  submitOtp(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.otpForm.markAllAsTouched();
    if (this.otpForm.invalid || this.loading()) {
      return;
    }
    this.otpValue = this.otpForm.controls.otp.value.trim();
    this.submitted.set(false);
    this.step.set(3);
  }

  submitPassword(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.infoMessage.set(null);
    this.passwordForm.markAllAsTouched();
    if (this.passwordForm.invalid || this.passwordMismatch() || this.loading()) {
      return;
    }

    const newPassword = this.passwordForm.controls.password.value;
    this.loading.set(true);
    this.auth
      .resetPassword({
        email: this.emailValue,
        otp: this.otpValue,
        newPassword,
      })
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          this.infoMessage.set(res.message);
          void this.router.navigateByUrl('/auth/login');
        },
        error: (err: Error) => {
          this.loading.set(false);
          this.errorMessage.set(err.message || 'Could not reset password.');
          if (/invalid or expired/i.test(err.message || '')) {
            this.step.set(2);
          }
        },
      });
  }
}
