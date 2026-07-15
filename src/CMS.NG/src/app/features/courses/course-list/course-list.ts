import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { TableModule, TablePageEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';

import { Course, CourseQuery } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';
import { toIso } from '@core/utils/date.util';

const FILTERS_KEY = 'course-list-filters';
const SORT_KEY = 'course-list-sort';
const PAGE_KEY = 'course-list-page';

interface CanRepeatOption {
  label: string;
  value: boolean | null;
}

@Component({
  selector: 'course-list',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TableModule,
    ButtonModule,
    DrawerModule,
    InputTextModule,
    SelectModule,
    DatePickerModule,
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
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入選單資料。' });
        this.restoreState();
        this.load();
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
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入課程資料。' });
        this.loading.set(false);
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
      error: () => {
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除課程時發生錯誤。' });
      },
    });
  }
}
