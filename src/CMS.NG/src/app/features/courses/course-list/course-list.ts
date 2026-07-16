import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';
import { TableModule, TablePageEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';

import { Course, CourseQuery, CourseRequest } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';
import { toIso, fromIso } from '@core/utils/date.util';
import { isServerError } from '@core/utils/http-error.util';

const FILTERS_KEY = 'course-list-filters';
const SORT_KEY = 'course-list-sort';
const PAGE_KEY = 'course-list-page';

interface CanRepeatOption {
  label: string;
  value: boolean | null;
}

/**
 * Columns the list table allows inline editing on. Every list column is here EXCEPT the
 * three read-only ones — `pkid` (primary key), `partner.name` and `courseGroup.description`
 * (FK lookups) — which are never made editable.
 */
type EditableField =
  | 'displayOrder'
  | 'courseId'
  | 'prodCourseId'
  | 'title'
  | 'publishStatusPkid'
  | 'scheduleOn'
  | 'scheduleOff'
  | 'hour'
  | 'listPrice'
  | 'learningCredit'
  | 'canRepeat';

/** The working value held while a single cell is being edited (type varies by column). */
type EditValue = string | number | boolean | Date | null;

/** Which cell is currently open for editing. */
interface EditingCell {
  pkid: number;
  field: EditableField;
}

@Component({
  selector: 'course-list',
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    DatePickerModule,
    CheckboxModule,
    TagModule,
    TooltipModule,
    ToastModule,
    ConfirmDialogModule,
  ],
  providers: [ConfirmationService, MessageService],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss',
})
export class CourseList implements OnInit {
  private readonly service = inject(CourseService);
  private readonly lookups = inject(LookupService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly courses = signal<Course[]>([]);
  protected readonly loading = signal(false);
  protected readonly filterVisible = signal(false);

  protected readonly partners = signal<PartnerLookup[]>([]);
  protected readonly courseGroups = signal<CourseGroupLookup[]>([]);
  protected readonly publishStatuses = signal<PublishStatusLookup[]>([]);

  // --- Inline editing state ------------------------------------------------
  // Which cell is open for editing, the working value, and the current inline error.
  protected readonly editingCell = signal<EditingCell | null>(null);
  protected readonly editError = signal<string | null>(null);
  // Heterogeneous edit buffer (text / number / date / select / checkbox), hence `any`
  // so the single `[(ngModel)]` target satisfies strictTemplates across every editor.
  protected editValue: EditValue = null;
  // Guards against a blur firing a second save while an update request is in flight.
  private savingCell = false;

  protected readonly canRepeatOptions: CanRepeatOption[] = [
    { label: '全部', value: null },
    { label: '允許重聽', value: true },
    { label: '不可重聽', value: false },
  ];

  // Drawer date pickers bind to Date; converted to ISO strings on apply.
  protected scheduleOnFrom: Date | null = null;
  protected scheduleOnTo: Date | null = null;
  protected scheduleOffFrom: Date | null = null;
  protected scheduleOffTo: Date | null = null;

  protected filters: CourseQuery = this.emptyFilters();
  protected sortField = 'displayOrder';
  protected sortOrder = 1;
  protected first = 0;
  protected rows = 20;

  ngOnInit(): void {
    forkJoin({
      partners: this.lookups.getPartners(),
      courseGroups: this.lookups.getCourseGroups(),
      publishStatuses: this.lookups.getPublishStatuses(),
    }).subscribe({
      next: ({ partners, courseGroups, publishStatuses }) => {
        this.partners.set(partners);
        this.courseGroups.set(courseGroups);
        this.publishStatuses.set(publishStatuses);
        this.restoreState();
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.restoreState();
        this.load();
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入選單資料。' });
      },
    });
  }

  private emptyFilters(): CourseQuery {
    return {
      keyword: null,
      partnerPkid: null,
      courseGroupPkid: null,
      publishStatusPkid: null,
      canRepeat: null,
      scheduleOnFrom: null,
      scheduleOnTo: null,
      scheduleOffFrom: null,
      scheduleOffTo: null,
    };
  }

  private restoreState(): void {
    const savedFilters = sessionStorage.getItem(FILTERS_KEY);
    if (savedFilters) {
      this.filters = { ...this.emptyFilters(), ...JSON.parse(savedFilters) };
    }
    // Cross-entity nav (e.g. from Partner detail) overrides the saved Partner filter.
    const partnerPkidParam = this.route.snapshot.queryParamMap.get('partnerPkid');
    if (partnerPkidParam) {
      this.filters.partnerPkid = Number(partnerPkidParam);
    }
    const savedSort = sessionStorage.getItem(SORT_KEY);
    if (savedSort) {
      const { sortField, sortOrder } = JSON.parse(savedSort);
      this.sortField = sortField;
      this.sortOrder = sortOrder;
    }
    const savedPage = sessionStorage.getItem(PAGE_KEY);
    if (savedPage) {
      const { first, rows } = JSON.parse(savedPage);
      this.first = first;
      this.rows = rows;
    }
  }

  load(): void {
    this.loading.set(true);
    this.service.query(this.filters).subscribe({
      next: (data) => {
        this.courses.set(data);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入課程資料。' });
      },
    });
  }

  applyFilters(): void {
    this.filters.scheduleOnFrom = toIso(this.scheduleOnFrom);
    this.filters.scheduleOnTo = toIso(this.scheduleOnTo);
    this.filters.scheduleOffFrom = toIso(this.scheduleOffFrom);
    this.filters.scheduleOffTo = toIso(this.scheduleOffTo);
    sessionStorage.setItem(FILTERS_KEY, JSON.stringify(this.filters));
    this.first = 0;
    this.persistPage();
    this.filterVisible.set(false);
    this.load();
  }

  clearFilters(): void {
    this.filters = this.emptyFilters();
    this.scheduleOnFrom = this.scheduleOnTo = this.scheduleOffFrom = this.scheduleOffTo = null;
    sessionStorage.removeItem(FILTERS_KEY);
    this.load();
  }

  onSort(event: { field?: string | string[] | null; order?: number | null }): void {
    if (typeof event.field === 'string') {
      this.sortField = event.field;
      this.sortOrder = event.order ?? 1;
      sessionStorage.setItem(SORT_KEY, JSON.stringify({ sortField: this.sortField, sortOrder: this.sortOrder }));
    }
  }

  onPage(event: TablePageEvent): void {
    this.first = event.first;
    this.rows = event.rows;
    this.persistPage();
  }

  private persistPage(): void {
    sessionStorage.setItem(PAGE_KEY, JSON.stringify({ first: this.first, rows: this.rows }));
  }

  // --- Inline editing ------------------------------------------------------

  isEditing(course: Course, field: EditableField): boolean {
    const cell = this.editingCell();
    return !!cell && cell.pkid === course.pkid && cell.field === field;
  }

  /** Double-click handler: open the given cell for editing (single-click never calls this). */
  startEdit(course: Course, field: EditableField): void {
    if (this.savingCell) return;
    this.editValue = this.initialValue(course, field);
    this.editError.set(null);
    this.editingCell.set({ pkid: course.pkid, field });
  }

  cancelEdit(): void {
    this.editingCell.set(null);
    this.editError.set(null);
    this.editValue = null;
  }

  /**
   * Blur / change handler: validate the working value and persist it via the Course update
   * endpoint. On validation failure show an inline error and keep the cell in edit mode; on a
   * failed save revert to the previous value and surface the error.
   */
  onEditBlur(course: Course, field: EditableField): void {
    if (!this.isEditing(course, field) || this.savingCell) return;

    const error = this.validate(course, field, this.editValue);
    if (error) {
      this.editError.set(error); // keep the cell in edit mode so the user can fix it
      return;
    }

    const value = this.normalize(field, this.editValue);
    if (value === this.currentValue(course, field)) {
      this.cancelEdit(); // unchanged — nothing to persist
      return;
    }

    const updated = this.applyField(course, field, value);
    this.savingCell = true;
    this.service.update(this.toRequest(updated)).subscribe({
      next: () => {
        this.savingCell = false;
        this.courses.update((list) => list.map((c) => (c.pkid === updated.pkid ? updated : c)));
        this.cancelEdit();
        this.messages.add({ severity: 'success', summary: '已更新', detail: '課程資料已更新。' });
      },
      error: (err: HttpErrorResponse) => {
        this.savingCell = false;
        // Revert: the row in the signal was never mutated, so closing the editor restores it.
        this.cancelEdit();
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '更新失敗', detail: '儲存變更時發生錯誤，已還原。' });
      },
    });
  }

  /** Seed the edit buffer from the row; date columns bind to a Date, everything else to its raw value. */
  private initialValue(course: Course, field: EditableField): EditValue {
    switch (field) {
      case 'scheduleOn':
        return fromIso(course.scheduleOn);
      case 'scheduleOff':
        return fromIso(course.scheduleOff);
      default:
        return course[field] as EditValue;
    }
  }

  /** The row's persisted value for `field`, in the same normalized shape `normalize()` produces. */
  private currentValue(course: Course, field: EditableField): EditValue {
    switch (field) {
      case 'scheduleOn':
        return course.scheduleOn.slice(0, 10);
      case 'scheduleOff':
        return course.scheduleOff.slice(0, 10);
      default:
        return course[field] as EditValue;
    }
  }

  /** Validate a candidate value for `field`; returns an error message, or null when valid. */
  private validate(course: Course, field: EditableField, value: EditValue): string | null {
    switch (field) {
      case 'title':
      case 'courseId':
      case 'prodCourseId':
        return ((value as string | null) ?? '').trim() ? null : '此欄位為必填，不可清空。';
      case 'displayOrder':
      case 'hour':
      case 'listPrice':
      case 'learningCredit': {
        if (value === null || value === undefined || (value as unknown) === '') {
          return '此欄位為必填，不可清空。';
        }
        const n = Number(value);
        if (Number.isNaN(n)) return '請輸入有效數字。';
        if (n < 0) return '數值不可為負數。';
        return null;
      }
      case 'publishStatusPkid':
        return value === null || value === undefined ? '請選擇上架狀態。' : null;
      case 'scheduleOn':
      case 'scheduleOff': {
        const d = value as Date | null;
        if (!(d instanceof Date) || Number.isNaN(d.getTime())) return '請輸入有效日期。';
        const iso = toIso(d)!;
        const onIso = field === 'scheduleOn' ? iso : course.scheduleOn.slice(0, 10);
        const offIso = field === 'scheduleOff' ? iso : course.scheduleOff.slice(0, 10);
        return onIso > offIso ? '上架日期不可晚於下架日期。' : null;
      }
      case 'canRepeat':
        return null;
    }
  }

  /** Coerce the raw edit buffer into the shape stored on the Course row. */
  private normalize(field: EditableField, value: EditValue): EditValue {
    switch (field) {
      case 'scheduleOn':
      case 'scheduleOff':
        return toIso(value as Date);
      case 'displayOrder':
      case 'hour':
      case 'listPrice':
      case 'learningCredit':
      case 'publishStatusPkid':
        return Number(value);
      case 'canRepeat':
        return value as boolean;
      default:
        return (value as string).trim();
    }
  }

  /** Return a copy of `course` with `field` set to the normalized `value`. */
  private applyField(course: Course, field: EditableField, value: EditValue): Course {
    const updated: Course = { ...course };
    switch (field) {
      case 'title':
        updated.title = value as string;
        break;
      case 'courseId':
        updated.courseId = value as string;
        break;
      case 'prodCourseId':
        updated.prodCourseId = value as string;
        break;
      case 'displayOrder':
        updated.displayOrder = value as number;
        break;
      case 'hour':
        updated.hour = value as number;
        break;
      case 'listPrice':
        updated.listPrice = value as number;
        break;
      case 'learningCredit':
        updated.learningCredit = value as number;
        break;
      case 'scheduleOn':
        updated.scheduleOn = value as string;
        break;
      case 'scheduleOff':
        updated.scheduleOff = value as string;
        break;
      case 'canRepeat':
        updated.canRepeat = value as boolean;
        break;
      case 'publishStatusPkid':
        updated.publishStatusPkid = value as number;
        updated.publishStatus =
          this.publishStatuses().find((s) => s.pkid === value) ?? course.publishStatus;
        break;
    }
    return updated;
  }

  /** Build the write DTO from a (possibly edited) Course row. */
  private toRequest(c: Course): CourseRequest {
    return {
      pkid: c.pkid,
      title: c.title,
      officialTitle: c.officialTitle,
      courseId: c.courseId,
      prodCourseId: c.prodCourseId,
      friendlyUrl: c.friendlyUrl,
      displayOrder: c.displayOrder,
      partnerPkid: c.partnerPkid,
      courseGroupPkid: c.courseGroupPkid,
      publishStatusPkid: c.publishStatusPkid,
      scheduleOn: c.scheduleOn.slice(0, 10),
      scheduleOff: c.scheduleOff.slice(0, 10),
      hour: c.hour,
      listPrice: c.listPrice,
      learningCredit: c.learningCredit,
      material: c.material,
      objective: c.objective,
      target: c.target,
      prerequisites: c.prerequisites,
      outline: c.outline,
      towardCertOrExam: c.towardCertOrExam,
      note: c.note,
      otherInfo: c.otherInfo,
      canRepeat: c.canRepeat,
      jobCategoryPkids: c.jobCategoryPkids ?? [],
      certificationPkids: c.certificationPkids ?? [],
    };
  }

  add(): void {
    this.router.navigate(['/courses/new']);
  }

  view(course: Course): void {
    this.router.navigate(['/courses', course.pkid]);
  }

  edit(course: Course): void {
    this.router.navigate(['/courses', course.pkid, 'edit']);
  }

  confirmDelete(course: Course): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除主代碼 <b>${course.pkid}</b>「${course.title}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.delete(course),
    });
  }

  private delete(course: Course): void {
    this.service.delete(course.pkid).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已刪除', detail: `課程「${course.title}」已刪除。` });
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除課程時發生錯誤。' });
      },
    });
  }
}
