import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AuthService } from '@core/services/auth.service';
import { isServerError } from '@core/utils/http-error.util';

/**
 * New-password complexity message, kept identical to the server's `PasswordPolicy.ComplexityMessage`
 * so the same wording appears whether the client or the API rejects the password.
 */
export const PASSWORD_COMPLEXITY_MESSAGE =
  '密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號 ' +
  '(Password must be at least 8 characters and contain at least 3 of the 4 classes: ' +
  'uppercase / lowercase / digit / symbol.)';

/**
 * Validator mirroring the backend rule: at least 8 characters AND at least 3 of the 4 character
 * classes (uppercase / lowercase / digit / symbol). Empty is left to `Validators.required`.
 */
export function passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value as string) ?? '';
  if (!value) {
    return null;
  }
  const classes =
    (/[A-Z]/.test(value) ? 1 : 0) +
    (/[a-z]/.test(value) ? 1 : 0) +
    (/[0-9]/.test(value) ? 1 : 0) +
    (/[^A-Za-z0-9]/.test(value) ? 1 : 0);
  return value.length >= 8 && classes >= 3 ? null : { complexity: true };
}

/** Group validator: the confirmation must equal the new password once both are filled in. */
export function passwordsMatchValidator(group: AbstractControl): ValidationErrors | null {
  const next = group.get('newPassword')?.value as string;
  const confirm = group.get('confirmNewPassword')?.value as string;
  if (!next || !confirm) {
    return null;
  }
  return next === confirm ? null : { mismatch: true };
}

/**
 * "My Profile" page for the signed-in user. UserId and roles are read-only (sourced from the
 * session/JWT via {@link AuthService}); only UserName is editable and is saved through
 * {@link AuthService.updateProfile}, which the backend applies to the JWT user only.
 */
@Component({
  selector: 'app-profile',
  imports: [CommonModule, ReactiveFormsModule, ButtonModule, InputTextModule, TagModule, ToastModule],
  providers: [MessageService],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile {
  private readonly fb = inject(FormBuilder);
  protected readonly auth = inject(AuthService);
  private readonly messages = inject(MessageService);

  protected readonly saving = signal(false);
  protected readonly changingPassword = signal(false);

  /** Message shown under the new-password field; matches the server's wording. */
  protected readonly complexityMessage = PASSWORD_COMPLEXITY_MESSAGE;

  protected readonly form = this.fb.group({
    userName: this.fb.nonNullable.control(this.auth.userName() ?? '', [
      Validators.required,
      Validators.maxLength(200),
    ]),
  });

  protected readonly passwordForm = this.fb.group(
    {
      currentPassword: this.fb.nonNullable.control('', [Validators.required]),
      newPassword: this.fb.nonNullable.control('', [
        Validators.required,
        passwordComplexityValidator,
      ]),
      confirmNewPassword: this.fb.nonNullable.control('', [Validators.required]),
    },
    { validators: passwordsMatchValidator },
  );

  save(): void {
    // Trim before validating so a whitespace-only name is rejected client-side too.
    const userName = this.form.controls.userName.value.trim();
    this.form.controls.userName.setValue(userName);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.auth.updateProfile(userName).subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'success', summary: '已儲存', detail: '個人資料已更新。' });
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        const detail =
          err.status === 400 ? '使用者名稱為必填。' : '儲存時發生錯誤，請稍後再試。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.changingPassword.set(true);
    this.auth.changePassword(this.passwordForm.getRawValue()).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordForm.reset();
        this.messages.add({ severity: 'success', summary: '已變更', detail: '密碼已更新。' });
      },
      error: (err: HttpErrorResponse) => {
        this.changingPassword.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        // Prefer the server's message (e.g. wrong current password / complexity) when present.
        const detail =
          (typeof err.error === 'object' && err.error?.message) ||
          (err.status === 400 ? '密碼變更失敗，請檢查輸入。' : '變更時發生錯誤，請稍後再試。');
        this.messages.add({ severity: 'error', summary: '變更失敗', detail });
      },
    });
  }
}
