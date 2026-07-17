import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { PartnerRequest } from '@core/models/partner.model';
import { PartnerService } from '@core/services/partner.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';
import { isServerError } from '@core/utils/http-error.util';

@Component({
  selector: 'partner-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    ToastModule,
    RowAuditBadge,
  ],
  providers: [MessageService],
  templateUrl: './partner-form.html',
  styleUrl: './partner-form.scss',
})
export class PartnerForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(PartnerService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  /** The edited record's pkid for the audit-history badge (0 in add mode → no history). */
  protected readonly auditPkid = signal(0);

  private pkid = 0;

  protected readonly form = this.fb.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    appKey: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(10)]),
    nameOnPartnerMenu: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    nameOnCourseDetailPage: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    displayOrder: this.fb.nonNullable.control<number | null>(null, [Validators.required]),
    imageFilename: this.fb.control<string | null>(null, [Validators.maxLength(50)]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!idParam);

    if (idParam) {
      const pkid = Number(idParam);
      this.service.getById(pkid).subscribe({
        next: (partner) => {
          this.pkid = partner.pkid;
          this.auditPkid.set(partner.pkid);
          this.form.patchValue({
            name: partner.name,
            appKey: partner.appKey,
            nameOnPartnerMenu: partner.nameOnPartnerMenu,
            nameOnCourseDetailPage: partner.nameOnCourseDetailPage,
            displayOrder: partner.displayOrder,
            imageFilename: partner.imageFilename,
          });
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          if (isServerError(err)) return; // the interceptor already reported this
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
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
    const request: PartnerRequest = {
      pkid: this.pkid,
      name: raw.name,
      appKey: raw.appKey,
      nameOnPartnerMenu: raw.nameOnPartnerMenu,
      nameOnCourseDetailPage: raw.nameOnCourseDetailPage,
      displayOrder: raw.displayOrder!,
      imageFilename: raw.imageFilename,
    };

    this.saving.set(true);
    const onError = (err: HttpErrorResponse) => {
      this.saving.set(false);
      if (isServerError(err)) return; // the interceptor already reported this
      this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存廠商時發生錯誤。' });
    };
    const onSuccess = (pkid: number) => {
      this.messages.add({ severity: 'success', summary: '已儲存', detail: '廠商資料已儲存。' });
      this.saving.set(false);
      this.router.navigate(['/partners', pkid]);
    };

    if (this.isEdit()) {
      // pkid is immutable on edit — navigate back to the same record.
      this.service.update(request).subscribe({ next: () => onSuccess(request.pkid), error: onError });
    } else {
      // pkid is IDENTITY — use the server-assigned pkid from the create response.
      this.service.create(request).subscribe({ next: (created) => onSuccess(created.pkid), error: onError });
    }
  }

  cancel(): void {
    if (this.isEdit()) {
      this.router.navigate(['/partners', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/partners']);
    }
  }
}
