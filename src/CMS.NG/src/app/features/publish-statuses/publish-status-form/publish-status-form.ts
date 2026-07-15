import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { PublishStatusRequest } from '@core/models/publish-status.model';
import { PublishStatusService } from '@core/services/publish-status.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'publish-status-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    CheckboxModule,
    ToastModule,
    RowAuditBadge,
  ],
  providers: [MessageService],
  templateUrl: './publish-status-form.html',
  styleUrl: './publish-status-form.scss',
})
export class PublishStatusForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PublishStatusService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  /** The edited record's pkid for the audit-history badge (0 in add mode → no history). */
  protected readonly auditPkid = signal(0);

  protected readonly form = this.fb.group({
    pkid: this.fb.nonNullable.control<number | null>(null, [Validators.required]),
    description: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    isDraft: this.fb.nonNullable.control(false),
    isPublished: this.fb.nonNullable.control(false),
    isDiscontinued: this.fb.nonNullable.control(false),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!idParam);

    if (idParam) {
      const pkid = Number(idParam);
      this.service.getById(pkid).subscribe({
        next: (status) => {
          this.auditPkid.set(status.pkid);
          this.form.patchValue({
            pkid: status.pkid,
            description: status.description,
            isDraft: status.isDraft,
            isPublished: status.isPublished,
            isDiscontinued: status.isDiscontinued,
          });
          this.form.controls.pkid.disable(); // pkid is immutable in edit mode.
          this.loading.set(false);
        },
        error: () => {
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
          this.loading.set(false);
        },
      });
    } else {
      this.loading.set(false);
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: PublishStatusRequest = {
      pkid: raw.pkid!,
      description: raw.description,
      isDraft: raw.isDraft,
      isPublished: raw.isPublished,
      isDiscontinued: raw.isDiscontinued,
    };

    this.saving.set(true);
    const op$: Observable<unknown> = this.isEdit()
      ? this.service.update(request)
      : this.service.create(request);
    op$.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已儲存', detail: '發布狀態資料已儲存。' });
        this.saving.set(false);
        this.router.navigate(['/publish-statuses', request.pkid]);
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const detail =
          err.status === 409 ? (err.error?.message ?? '主代碼已存在。') : '儲存發布狀態時發生錯誤。';
        this.messages.add({ severity: 'error', summary: '儲存失敗', detail });
      },
    });
  }

  cancel(): void {
    if (this.isEdit()) {
      this.router.navigate(['/publish-statuses', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/publish-statuses']);
    }
  }
}
