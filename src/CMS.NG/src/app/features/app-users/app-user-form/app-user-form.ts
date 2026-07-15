import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of, Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUserRequest } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AuthService } from '@core/services/auth.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-user-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    CheckboxModule,
    MultiSelectModule,
    ToastModule,
    ConfirmDialogModule,
    RowAuditBadge,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './app-user-form.html',
  styleUrl: './app-user-form.scss',
})
export class AppUserForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly lookups = inject(LookupService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);
  private readonly auth = inject(AuthService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly roles = signal<AppRoleLookup[]>([]);

  /** Whether the signed-in user may reset passwords — mirrors the Admin gate the API enforces. */
  protected readonly isAdmin = computed(() => this.auth.hasRole('Admin'));
  /** The edited record's pkid for the audit-history badge (0 in add mode → no history). */
  protected readonly auditPkid = signal(0);

  private pkid = 0;

  protected readonly form = this.fb.group({
    userId: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    userName: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    isActive: this.fb.nonNullable.control(true),
    roleIds: this.fb.nonNullable.control<string[]>([]),
  });

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!userId);

    forkJoin({
      roles: this.lookups.getAppRoles(),
      user: userId ? this.service.getById(userId) : of(null),
    }).subscribe({
      next: ({ roles, user }) => {
        this.roles.set(roles);
        if (user) {
          this.pkid = user.pkid;
          this.auditPkid.set(user.pkid);
          this.form.patchValue({
            userId: user.userId,
            userName: user.userName,
            isActive: user.isActive,
            roleIds: user.roleIds,
          });
          this.form.controls.userId.disable(); // UserId is immutable in edit mode.
        }
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
        this.loading.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: AppUserRequest = {
      pkid: this.pkid,
      userId: raw.userId,
      userName: raw.userName,
      isActive: raw.isActive,
      roleIds: raw.roleIds,
    };

    this.saving.set(true);
    const op$: Observable<unknown> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);
    op$.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: '使用者資料已儲存。' });
        this.saving.set(false);
        this.router.navigate(['/app-users', request.userId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? (err.error?.message ?? '使用者代碼已存在。') : '儲存使用者時發生錯誤。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  /**
   * Ask for confirmation, then reset the edited user's password to the system default. Admin-only —
   * the button is hidden for non-Admins and the API enforces the same Admin gate (403 otherwise).
   * Only the UserId is sent; no password or hash ever crosses the wire in either direction.
   */
  confirmResetPassword(): void {
    const userId = this.form.getRawValue().userId;
    if (!this.isEdit() || !this.isAdmin() || !userId) return;

    this.confirmation.confirm({
      header: '重設密碼',
      message: `確定要將使用者「${userId}」的密碼重設為系統預設密碼？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => this.resetPassword(userId),
    });
  }

  private resetPassword(userId: string): void {
    this.service.resetPassword(userId).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已重設', detail: '密碼已重設為系統預設密碼。' });
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '重設失敗', detail: '重設密碼時發生錯誤。' });
      },
    });
  }

  cancel(): void {
    if (this.isEdit()) {
      this.router.navigate(['/app-users', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/app-users']);
    }
  }
}
