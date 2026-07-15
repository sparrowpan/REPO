import { Component, OnInit, inject, signal } from '@angular/core';
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
import { MessageService } from 'primeng/api';

import { AppUserRequest } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';

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
  ],
  providers: [MessageService],
  templateUrl: './app-user-form.html',
  styleUrl: './app-user-form.scss',
})
export class AppUserForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly roles = signal<AppRoleLookup[]>([]);

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

  cancel(): void {
    if (this.isEdit()) {
      this.router.navigate(['/app-users', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/app-users']);
    }
  }
}
