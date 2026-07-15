import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { CourseGroupList } from './course-group-list';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

describe('CourseGroupList', () => {
  let fixture: ComponentFixture<CourseGroupList>;
  let component: CourseGroupList;
  let serviceSpy: jasmine.SpyObj<CourseGroupService>;

  const courseGroups: CourseGroup[] = [
    { pkid: 1, description: '資訊技術' },
    { pkid: 2, description: '商業管理' },
  ];

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(courseGroups));
    serviceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [CourseGroupList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CourseGroupService, useValue: serviceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseGroupList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads course groups on init via query()', () => {
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['courseGroups']().length).toBe(2);
  });

  it('renders a row per course group', () => {
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilters persists filters to sessionStorage and reloads', () => {
    component['filters'] = { keyword: '資訊' };
    component.applyFilters();
    expect(sessionStorage.getItem('course-group-list-filters')).toContain('資訊');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('clearFilters resets filters and removes the stored key', () => {
    sessionStorage.setItem('course-group-list-filters', '{"keyword":"x"}');
    component.clearFilters();
    expect(sessionStorage.getItem('course-group-list-filters')).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.view(courseGroups[0]);
    expect(navSpy).toHaveBeenCalledWith(['/course-groups', 1]);
  });

  it('confirmDelete accepting deletes the course group and reloads', () => {
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmDelete(courseGroups[1]);
    expect(serviceSpy.delete).toHaveBeenCalledWith(2);
  });

  it('surfaces a load error without throwing', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBe(false);
  });
});
