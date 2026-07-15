import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, of, Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MultiSelectModule } from 'primeng/multiselect';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AppRoleRequest } from '@core/models/app-role.model';
import { AppUserLookup } from '@core/models/app-user-lookup.model';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'app-role-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    MultiSelectModule,
    ToastModule,
    RowAuditBadge,
  ],
  providers: [MessageService],
  templateUrl: './app-role-form.html',
  styleUrl: './app-role-form.scss',
})
export class AppRoleForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly users = signal<AppUserLookup[]>([]);
  /** The edited record's pkid for the audit-history badge (0 in add mode → no history). */
  protected readonly auditPkid = signal(0);

  private pkid = 0;

  protected readonly form = this.fb.group({
    roleId: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    roleName: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    permissionLevel: this.fb.nonNullable.control(100, [Validators.required]),
    description: this.fb.control<string | null>(null, [Validators.maxLength(400)]),
    userIds: this.fb.nonNullable.control<string[]>([]),
  });

  ngOnInit(): void {
    const roleId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!roleId);

    forkJoin({
      users: this.lookups.getAppUsers(),
      role: roleId ? this.service.getById(roleId) : of(null),
    }).subscribe({
      next: ({ users, role }) => {
        this.users.set(users);
        if (role) {
          this.pkid = role.pkid;
          this.auditPkid.set(role.pkid);
          this.form.patchValue({
            roleId: role.roleId,
            roleName: role.roleName,
            permissionLevel: role.permissionLevel,
            description: role.description,
            userIds: role.userIds,
          });
          this.form.controls.roleId.disable(); // RoleId is immutable in edit mode.
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
    const request: AppRoleRequest = {
      pkid: this.pkid,
      roleId: raw.roleId,
      roleName: raw.roleName,
      permissionLevel: raw.permissionLevel,
      description: raw.description,
      userIds: raw.userIds,
    };

    this.saving.set(true);
    const op$: Observable<unknown> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);
    op$.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: '角色資料已儲存。' });
        this.saving.set(false);
        this.router.navigate(['/app-roles', request.roleId]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? (err.error?.message ?? '角色代碼已存在。') : '儲存角色時發生錯誤。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    if (this.isEdit()) {
      this.router.navigate(['/app-roles', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/app-roles']);
    }
  }
}
