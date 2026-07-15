import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { CourseList } from './course-list';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';

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
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(courses));
    serviceSpy.delete.and.returnValue(of(void 0));

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
