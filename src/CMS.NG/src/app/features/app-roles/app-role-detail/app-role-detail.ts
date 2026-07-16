import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { AppRole } from '@core/models/app-role.model';
import { AppUserLookup } from '@core/models/app-user-lookup.model';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';
import { isServerError } from '@core/utils/http-error.util';

@Component({
  selector: 'app-role-detail',
  imports: [CommonModule, ButtonModule, TagModule, ToastModule, RowAuditBadge],
  providers: [MessageService],
  templateUrl: './app-role-detail.html',
  styleUrl: './app-role-detail.scss',
})
export class AppRoleDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(AppRoleService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly role = signal<AppRole | null>(null);
  protected readonly userLabels = signal<string[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const roleId = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      role: this.service.getById(roleId),
      users: this.lookups.getAppUsers(),
    }).subscribe({
      next: ({ role, users }) => {
        this.role.set(role);
        this.userLabels.set(this.mapUserLabels(role.userIds, users));
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入角色資料。' });
      },
    });
  }

  private mapUserLabels(userIds: string[], users: AppUserLookup[]): string[] {
    const byId = new Map(users.map((u) => [u.userId, u.label]));
    return userIds.map((id) => byId.get(id) ?? id);
  }

  edit(): void {
    this.router.navigate(['/app-roles', this.role()!.roleId, 'edit']);
  }

  back(): void {
    this.router.navigate(['/app-roles']);
  }
}
