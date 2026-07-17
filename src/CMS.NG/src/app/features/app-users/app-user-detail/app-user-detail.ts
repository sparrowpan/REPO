import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AppUser } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AuthService } from '@core/services/auth.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';
import { isServerError } from '@core/utils/http-error.util';

@Component({
  selector: 'app-user-detail',
  imports: [CommonModule, ButtonModule, TagModule, ToastModule, ConfirmDialogModule, RowAuditBadge],
  providers: [ConfirmationService, MessageService],
  templateUrl: './app-user-detail.html',
  styleUrl: './app-user-detail.scss',
})
export class AppUserDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppUserService);
  private readonly lookups = inject(LookupService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);
  private readonly auth = inject(AuthService);

  protected readonly user = signal<AppUser | null>(null);
  protected readonly roleLabels = signal<string[]>([]);
  protected readonly loading = signal(true);

  /** Whether the signed-in user may reset passwords — mirrors the Admin gate the API enforces. */
  protected readonly isAdmin = computed(() => this.auth.hasRole('Admin'));

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      user: this.service.getById(userId),
      roles: this.lookups.getAppRoles(),
    }).subscribe({
      next: ({ user, roles }) => {
        this.user.set(user);
        this.roleLabels.set(this.mapRoleLabels(user.roleIds, roles));
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入使用者資料。' });
      },
    });
  }

  private mapRoleLabels(roleIds: string[], roles: AppRoleLookup[]): string[] {
    const byId = new Map(roles.map((r) => [r.roleId, r.label]));
    return roleIds.map((id) => byId.get(id) ?? id);
  }

  confirmResetPassword(): void {
    const user = this.user();
    if (!user || !this.isAdmin()) return;
    this.confirmation.confirm({
      header: '重設密碼',
      message: `確定要將使用者「${user.userId}」的密碼重設為預設密碼？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '重設',
      rejectLabel: '取消',
      accept: () => this.resetPassword(user),
    });
  }

  private resetPassword(user: AppUser): void {
    this.service.resetPassword(user.userId).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已重設', detail: '密碼已重設為預設密碼。' });
        this.service.getById(user.userId).subscribe((u) => this.user.set(u));
      },
      error: (err: HttpErrorResponse) => {
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '重設失敗', detail: '重設密碼時發生錯誤。' });
      },
    });
  }

  edit(): void {
    this.router.navigate(['/app-users', this.user()!.userId, 'edit']);
  }

  back(): void {
    this.router.navigate(['/app-users']);
  }
}
