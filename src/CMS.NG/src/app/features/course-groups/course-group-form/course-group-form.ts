import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { CourseGroupRequest } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'course-group-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    RowAuditBadge,
  ],
  providers: [MessageService],
  templateUrl: './course-group-form.html',
  styleUrl: './course-group-form.scss',
})
export class CourseGroupForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  /** The edited record's pkid for the audit-history badge (0 in add mode → no history). */
  protected readonly auditPkid = signal(0);

  private pkid = 0;

  protected readonly form = this.fb.group({
    description: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!idParam);

    if (idParam) {
      const pkid = Number(idParam);
      this.service.getById(pkid).subscribe({
        next: (courseGroup) => {
          this.pkid = courseGroup.pkid;
          this.auditPkid.set(courseGroup.pkid);
          this.form.patchValue({
            description: courseGroup.description,
          });
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
    const request: CourseGroupRequest = {
      pkid: this.pkid,
      description: raw.description,
    };

    this.saving.set(true);
    const onError = () => {
      this.saving.set(false);
      this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存課程群組時發生錯誤。' });
    };
    const onSuccess = (pkid: number) => {
      this.messages.add({ severity: 'success', summary: '已儲存', detail: '課程群組資料已儲存。' });
      this.saving.set(false);
      this.router.navigate(['/course-groups', pkid]);
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
      this.router.navigate(['/course-groups', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/course-groups']);
    }
  }
}
