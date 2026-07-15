import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of, Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { CourseRequest } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';
import { JobCategoryLookup } from '@core/models/job-category-lookup.model';
import { CertificationLookup } from '@core/models/certification-lookup.model';
import { toIso, fromIso } from '@core/utils/date.util';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'course-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    InputNumberModule,
    SelectModule,
    MultiSelectModule,
    DatePickerModule,
    CheckboxModule,
    ToastModule,
    RowAuditBadge,
  ],
  providers: [MessageService],
  templateUrl: './course-form.html',
  styleUrl: './course-form.scss',
})
export class CourseForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly messages = inject(MessageService);

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);

  protected readonly partners = signal<PartnerLookup[]>([]);
  protected readonly courseGroups = signal<CourseGroupLookup[]>([]);
  protected readonly publishStatuses = signal<PublishStatusLookup[]>([]);
  protected readonly jobCategories = signal<JobCategoryLookup[]>([]);
  protected readonly certifications = signal<CertificationLookup[]>([]);
  /** The edited record's pkid for the audit-history badge (0 in add mode → no history). */
  protected readonly auditPkid = signal(0);

  private pkid = 0;

  protected readonly form = this.fb.group({
    title: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(200)]),
    officialTitle: this.fb.control<string | null>(null, [Validators.maxLength(300)]),
    courseId: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    prodCourseId: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(50)]),
    friendlyUrl: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    displayOrder: this.fb.nonNullable.control<number | null>(null, [Validators.required]),
    partnerPkid: this.fb.nonNullable.control<number | null>(null, [Validators.required]),
    courseGroupPkid: this.fb.control<number | null>(null),
    publishStatusPkid: this.fb.nonNullable.control<number | null>(null, [Validators.required]),
    scheduleOn: this.fb.nonNullable.control<Date | null>(null, [Validators.required]),
    scheduleOff: this.fb.nonNullable.control<Date | null>(null, [Validators.required]),
    hour: this.fb.nonNullable.control<number | null>(0, [Validators.required]),
    listPrice: this.fb.nonNullable.control<number | null>(0, [Validators.required]),
    learningCredit: this.fb.nonNullable.control<number | null>(0, [Validators.required]),
    material: this.fb.control<string | null>(null, [Validators.maxLength(500)]),
    objective: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    target: this.fb.control<string | null>(null, [Validators.maxLength(500)]),
    prerequisites: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    outline: this.fb.control<string | null>(null),
    towardCertOrExam: this.fb.control<string | null>(null),
    note: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    otherInfo: this.fb.control<string | null>(null, [Validators.maxLength(4000)]),
    canRepeat: this.fb.nonNullable.control(false),
    jobCategoryPkids: this.fb.nonNullable.control<number[]>([]),
    certificationPkids: this.fb.nonNullable.control<number[]>([]),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!idParam);

    forkJoin({
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
      jobCategories: this.lookups.getJobCategories(),
      certifications: this.lookups.getCertifications(),
      course: idParam ? this.service.getById(Number(idParam)) : of(null),
    }).subscribe({
      next: ({ partners, courseGroups, publishStatuses, jobCategories, certifications, course }) => {
        this.partners.set(partners);
        this.courseGroups.set(courseGroups);
        this.publishStatuses.set(publishStatuses);
        this.jobCategories.set(jobCategories);
        this.certifications.set(certifications);

        if (course) {
          this.pkid = course.pkid;
          this.auditPkid.set(course.pkid);
          this.form.patchValue({
            title: course.title,
            officialTitle: course.officialTitle,
            courseId: course.courseId,
            prodCourseId: course.prodCourseId,
            friendlyUrl: course.friendlyUrl,
            displayOrder: course.displayOrder,
            partnerPkid: course.partnerPkid,
            courseGroupPkid: course.courseGroupPkid,
            publishStatusPkid: course.publishStatusPkid,
            scheduleOn: fromIso(course.scheduleOn),
            scheduleOff: fromIso(course.scheduleOff),
            hour: course.hour,
            listPrice: course.listPrice,
            learningCredit: course.learningCredit,
            material: course.material,
            objective: course.objective,
            target: course.target,
            prerequisites: course.prerequisites,
            outline: course.outline,
            towardCertOrExam: course.towardCertOrExam,
            note: course.note,
            otherInfo: course.otherInfo,
            canRepeat: course.canRepeat,
            jobCategoryPkids: course.jobCategoryPkids,
            certificationPkids: course.certificationPkids,
          });
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
    const request: CourseRequest = {
      pkid: this.pkid,
      title: raw.title,
      officialTitle: raw.officialTitle,
      courseId: raw.courseId,
      prodCourseId: raw.prodCourseId,
      friendlyUrl: raw.friendlyUrl,
      displayOrder: raw.displayOrder!,
      partnerPkid: raw.partnerPkid!,
      courseGroupPkid: raw.courseGroupPkid,
      publishStatusPkid: raw.publishStatusPkid!,
      scheduleOn: toIso(raw.scheduleOn)!,
      scheduleOff: toIso(raw.scheduleOff)!,
      hour: raw.hour ?? 0,
      listPrice: raw.listPrice ?? 0,
      learningCredit: raw.learningCredit ?? 0,
      material: raw.material,
      objective: raw.objective,
      target: raw.target,
      prerequisites: raw.prerequisites,
      outline: raw.outline,
      towardCertOrExam: raw.towardCertOrExam,
      note: raw.note,
      otherInfo: raw.otherInfo,
      canRepeat: raw.canRepeat,
      jobCategoryPkids: raw.jobCategoryPkids,
      certificationPkids: raw.certificationPkids,
    };

    this.saving.set(true);
    const onError = () => {
      this.saving.set(false);
      this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存課程時發生錯誤。' });
    };
    const onSuccess = (pkid: number) => {
      this.messages.add({ severity: 'success', summary: '已儲存', detail: '課程資料已儲存。' });
      this.saving.set(false);
      this.router.navigate(['/courses', pkid]);
    };

    if (this.isEdit()) {
      this.service.update(request).subscribe({ next: () => onSuccess(request.pkid), error: onError });
    } else {
      this.service.create(request).subscribe({ next: (created) => onSuccess(created.pkid), error: onError });
    }
  }

  cancel(): void {
    if (this.isEdit()) {
      this.router.navigate(['/courses', this.route.snapshot.paramMap.get('id')]);
    } else {
      this.router.navigate(['/courses']);
    }
  }
}
