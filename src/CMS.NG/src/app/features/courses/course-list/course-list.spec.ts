import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { CourseList } from './course-list';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';

const courses = [
  { pkid: 1, title: 'AZ-900', courseId: 'AZ900', partner: { pkid: 1, name: '微軟' } },
  { pkid: 2, title: 'PMP', courseId: 'PMP', partner: { pkid: 2, name: 'PMI' } },
] as Course[];

function mockLookups(): jasmine.SpyObj<LookupService> {
  const spy = jasmine.createSpyObj<LookupService>('LookupService', [
    'getPartners', 'getCourseGroups', 'getPublishStatuses',
  ]);
  spy.getPartners.and.returnValue(of([]));
  spy.getCourseGroups.and.returnValue(of([]));
  spy.getPublishStatuses.and.returnValue(of([]));
  return spy;
}

describe('CourseList', () => {
  let fixture: ComponentFixture<CourseList>;
  let component: CourseList;
  let serviceSpy: jasmine.SpyObj<CourseService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['query', 'delete', 'update']);
    serviceSpy.query.and.returnValue(of(courses));
    serviceSpy.delete.and.returnValue(of(void 0));
    serviceSpy.update.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [CourseList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CourseService, useValue: serviceSpy },
        { provide: LookupService, useValue: mockLookups() },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads courses on init via query()', () => {
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['courses']().length).toBe(2);
  });

  it('renders a row per course', () => {
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilters persists filters to sessionStorage and reloads', () => {
    component['filters'].keyword = 'AZ';
    component.applyFilters();
    expect(sessionStorage.getItem('course-list-filters')).toContain('AZ');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('clearFilters resets filters and removes the stored key', () => {
    sessionStorage.setItem('course-list-filters', '{"keyword":"x"}');
    component.clearFilters();
    expect(sessionStorage.getItem('course-list-filters')).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.view(courses[0]);
    expect(navSpy).toHaveBeenCalledWith(['/courses', 1]);
  });

  it('confirmDelete accepting deletes the course and reloads', () => {
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmDelete(courses[1]);
    expect(serviceSpy.delete).toHaveBeenCalledWith(2);
  });

  it('surfaces a load error without throwing', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBe(false);
  });
});

// --- Inline editing -------------------------------------------------------

const PUBLISH_STATUSES: PublishStatusLookup[] = [
  { pkid: 1, description: '上架' },
  { pkid: 2, description: '下架' },
];

/** A fully-populated Course row so toRequest()/validation never hit undefined fields. */
function makeCourse(over: Partial<Course> = {}): Course {
  return {
    pkid: 1,
    title: 'AZ-900',
    officialTitle: null,
    courseId: 'AZ900',
    prodCourseId: 'PROD-AZ900',
    friendlyUrl: 'az-900',
    displayOrder: 10,
    partnerPkid: 1,
    courseGroupPkid: 3,
    publishStatusPkid: 1,
    scheduleOn: '2025-01-10',
    scheduleOff: '2025-01-20',
    hour: 8,
    listPrice: 12000,
    learningCredit: 3,
    material: null,
    objective: null,
    target: null,
    prerequisites: null,
    outline: null,
    towardCertOrExam: null,
    note: null,
    otherInfo: null,
    canRepeat: false,
    partner: { pkid: 1, name: '微軟' },
    courseGroup: { pkid: 3, description: '雲端' },
    publishStatus: { pkid: 1, description: '上架' },
    jobCategoryCount: 0,
    certificationCount: 0,
    jobCategoryPkids: [],
    certificationPkids: [],
    jobCategories: [],
    certifications: [],
    ...over,
  } as Course;
}

describe('CourseList — inline editing', () => {
  let fixture: ComponentFixture<CourseList>;
  let component: CourseList;
  let serviceSpy: jasmine.SpyObj<CourseService>;

  function seed(course: Course = makeCourse()): Course {
    component['courses'].set([course]);
    component['publishStatuses'].set(PUBLISH_STATUSES);
    fixture.detectChanges();
    return course;
  }

  function firstRowCells(): HTMLTableCellElement[] {
    const row = (fixture.nativeElement as HTMLElement).querySelector('tbody tr')!;
    return Array.from(row.querySelectorAll('td'));
  }

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['query', 'delete', 'update']);
    serviceSpy.query.and.returnValue(of([]));
    serviceSpy.delete.and.returnValue(of(void 0));
    serviceSpy.update.and.returnValue(of(void 0));

    const lookups = jasmine.createSpyObj<LookupService>('LookupService', [
      'getPartners', 'getCourseGroups', 'getPublishStatuses',
    ]);
    lookups.getPartners.and.returnValue(of([]));
    lookups.getCourseGroups.and.returnValue(of([]));
    lookups.getPublishStatuses.and.returnValue(of(PUBLISH_STATUSES));

    await TestBed.configureTestingModule({
      imports: [CourseList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CourseService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookups },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('double-click enters edit mode (single-click does not)', () => {
    const course = seed();
    const displayOrderCell = firstRowCells()[1]; // 顯示順序

    displayOrderCell.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
    expect(component['editingCell']()).toBeNull();
    expect(displayOrderCell.querySelector('input')).toBeNull();

    displayOrderCell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    fixture.detectChanges();
    expect(component.isEditing(course, 'displayOrder')).toBe(true);
    expect(displayOrderCell.querySelector('input')).not.toBeNull();
  });

  it('does not edit the three read-only columns on double-click', () => {
    seed();
    const cells = firstRowCells();
    // 主代碼 (0), 原廠 (5), 課程群組 (6) are read-only.
    for (const index of [0, 5, 6]) {
      cells[index].dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
      fixture.detectChanges();
      expect(component['editingCell']())
        .withContext(`column index ${index} must stay read-only`)
        .toBeNull();
      expect(cells[index].querySelector('input')).toBeNull();
    }
  });

  it('read-only cells are not marked editable; the rest are', () => {
    seed();
    const cells = firstRowCells();
    const readOnly = [0, 5, 6];
    cells.slice(0, 14).forEach((cell, index) => {
      const editable = cell.classList.contains('editable');
      expect(editable)
        .withContext(`column index ${index}`)
        .toBe(!readOnly.includes(index));
    });
  });

  it('blur persists a valid change via the update endpoint and updates the row', () => {
    const course = seed();
    component.startEdit(course, 'displayOrder');
    component['editValue'] = 99;
    component.onEditBlur(course, 'displayOrder');

    expect(serviceSpy.update).toHaveBeenCalledTimes(1);
    expect(serviceSpy.update.calls.mostRecent().args[0].displayOrder).toBe(99);
    expect(component['courses']()[0].displayOrder).toBe(99);
    expect(component['editingCell']()).toBeNull();
  });

  it('blur editing 上架狀態 updates the pkid and the nested lookup label', () => {
    const course = seed();
    component.startEdit(course, 'publishStatusPkid');
    component['editValue'] = 2;
    component.onEditBlur(course, 'publishStatusPkid');

    expect(serviceSpy.update.calls.mostRecent().args[0].publishStatusPkid).toBe(2);
    expect(component['courses']()[0].publishStatus?.description).toBe('下架');
  });

  it('blur editing 允許重聽 persists the boolean', () => {
    const course = seed();
    component.startEdit(course, 'canRepeat');
    component['editValue'] = true;
    component.onEditBlur(course, 'canRepeat');

    expect(serviceSpy.update.calls.mostRecent().args[0].canRepeat).toBe(true);
    expect(component['courses']()[0].canRepeat).toBe(true);
  });

  it('an unchanged value on blur closes the editor without calling update', () => {
    const course = seed();
    component.startEdit(course, 'displayOrder');
    component.onEditBlur(course, 'displayOrder'); // value untouched

    expect(serviceSpy.update).not.toHaveBeenCalled();
    expect(component['editingCell']()).toBeNull();
  });

  describe('validation blocks invalid edits (inline error, stays in edit mode)', () => {
    it('rejects a cleared required text field', () => {
      const course = seed();
      component.startEdit(course, 'title');
      component['editValue'] = '   ';
      component.onEditBlur(course, 'title');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component.isEditing(course, 'title')).toBe(true);
      expect(component['editError']()).toBeTruthy();
    });

    it('rejects a negative numeric field', () => {
      const course = seed();
      component.startEdit(course, 'listPrice');
      component['editValue'] = -1;
      component.onEditBlur(course, 'listPrice');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component.isEditing(course, 'listPrice')).toBe(true);
      expect(component['editError']()).toBeTruthy();
    });

    it('rejects a cleared (null) required numeric field', () => {
      const course = seed();
      component.startEdit(course, 'hour');
      component['editValue'] = null;
      component.onEditBlur(course, 'hour');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component.isEditing(course, 'hour')).toBe(true);
    });

    it('rejects an invalid date', () => {
      const course = seed();
      component.startEdit(course, 'scheduleOn');
      component['editValue'] = new Date('not-a-date');
      component.onEditBlur(course, 'scheduleOn');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component.isEditing(course, 'scheduleOn')).toBe(true);
    });

    it('rejects 上架日期 later than 下架日期', () => {
      const course = seed(); // scheduleOff = 2025-01-20
      component.startEdit(course, 'scheduleOn');
      component['editValue'] = new Date(2025, 1, 1); // 2025-02-01 > 下架日期
      component.onEditBlur(course, 'scheduleOn');

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component.isEditing(course, 'scheduleOn')).toBe(true);
      expect(component['editError']()).toContain('上架日期');
    });

    it('accepts 上架日期 not after 下架日期', () => {
      const course = seed();
      component.startEdit(course, 'scheduleOn');
      component['editValue'] = new Date(2025, 0, 15); // 2025-01-15 <= 下架日期
      component.onEditBlur(course, 'scheduleOn');

      expect(serviceSpy.update).toHaveBeenCalledTimes(1);
      expect(serviceSpy.update.calls.mostRecent().args[0].scheduleOn).toBe('2025-01-15');
    });
  });

  it('reverts the cell and surfaces an error when the save fails', () => {
    serviceSpy.update.and.returnValue(throwError(() => new Error('boom')));
    const course = seed();
    component.startEdit(course, 'displayOrder');
    component['editValue'] = 77;
    component.onEditBlur(course, 'displayOrder');

    expect(serviceSpy.update).toHaveBeenCalledTimes(1);
    // Row reverted to the original value; editor closed.
    expect(component['courses']()[0].displayOrder).toBe(10);
    expect(component['editingCell']()).toBeNull();
  });
});
