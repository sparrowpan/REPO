import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { CourseForm } from './course-form';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';

const existing = {
  pkid: 2,
  title: 'PMP 專案管理',
  officialTitle: null,
  courseId: 'PMP',
  prodCourseId: 'PROD-PMP',
  friendlyUrl: 'pmp',
  displayOrder: 2,
  partnerPkid: 2,
  courseGroupPkid: null,
  publishStatusPkid: 20,
  scheduleOn: '2026-01-01',
  scheduleOff: '2030-12-31',
  hour: 8,
  listPrice: 12000,
  learningCredit: 3.5,
  material: null,
  objective: null,
  target: null,
  prerequisites: null,
  outline: null,
  towardCertOrExam: null,
  note: null,
  otherInfo: null,
  canRepeat: false,
  jobCategoryPkids: [],
  certificationPkids: [],
} as unknown as Course;

function mockLookups(): jasmine.SpyObj<LookupService> {
  const spy = jasmine.createSpyObj<LookupService>('LookupService', [
    'getPartners', 'getCourseGroups', 'getPublishStatuses', 'getJobCategories', 'getCertifications',
  ]);
  spy.getPartners.and.returnValue(of([{ pkid: 2, name: 'PMI' }]));
  spy.getCourseGroups.and.returnValue(of([]));
  spy.getPublishStatuses.and.returnValue(of([{ pkid: 20, description: '已上架' }]));
  spy.getJobCategories.and.returnValue(of([]));
  spy.getCertifications.and.returnValue(of([]));
  return spy;
}

function setup(id: string | null) {
  const serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['getById', 'create', 'update']);
  serviceSpy.getById.and.returnValue(of(existing));
  serviceSpy.create.and.returnValue(of({ ...existing, pkid: 9 }));
  serviceSpy.update.and.returnValue(of(void 0));

  TestBed.configureTestingModule({
    imports: [CourseForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: CourseService, useValue: serviceSpy },
      { provide: LookupService, useValue: mockLookups() },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } },
    ],
  });

  const fixture: ComponentFixture<CourseForm> = TestBed.createComponent(CourseForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy };
}

describe('CourseForm (add mode)', () => {
  it('starts empty and does not load a course', () => {
    const { component, serviceSpy } = setup(null);
    expect(serviceSpy.getById).not.toHaveBeenCalled();
    expect(component['isEdit']()).toBe(false);
  });

  it('does not save when required fields are missing', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].controls.title.touched).toBe(true);
    expect(component['form'].controls.courseId.touched).toBe(true);
  });

  it('creates a course and navigates using the server-assigned pkid', () => {
    const { component, serviceSpy } = setup(null);
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component['form'].patchValue({
      title: 'Terraform',
      courseId: 'TF101',
      prodCourseId: 'PROD-TF101',
      friendlyUrl: 'tf101',
      displayOrder: 5,
      partnerPkid: 2,
      publishStatusPkid: 20,
      scheduleOn: new Date(2026, 2, 1),
      scheduleOff: new Date(2031, 2, 1),
    });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/courses', 9]);
  });
});

describe('CourseForm sticky action toolbar', () => {
  function assertStickyToolbar(fixture: ComponentFixture<CourseForm>) {
    const el = fixture.nativeElement as HTMLElement;
    const header = el.querySelector('.page-header') as HTMLElement;
    expect(header).withContext('action toolbar renders').not.toBeNull();

    // Pinned/frozen so Save & Cancel stay visible while the form body scrolls.
    expect(getComputedStyle(header).position).toBe('sticky');

    // Save & Cancel remain present inside the toolbar.
    const buttons = header.querySelectorAll('.page-header__actions button');
    const labels = Array.from(buttons).map((b) => b.textContent?.trim() ?? '');
    expect(labels.some((t) => t.includes('儲存'))).withContext('Save present').toBe(true);
    expect(labels.some((t) => t.includes('取消'))).withContext('Cancel present').toBe(true);
  }

  it('renders a pinned toolbar with Save/Cancel on the New form', () => {
    const { fixture } = setup(null);
    assertStickyToolbar(fixture);
  });

  it('renders a pinned toolbar with Save/Cancel on the Edit form', () => {
    const { fixture } = setup('2');
    assertStickyToolbar(fixture);
  });

  it('keeps the toolbar pinned to the top while the form body scrolls', () => {
    const { fixture } = setup('2');

    // Reproduce the app shell's scroll region (.content in app.scss): a bounded,
    // internally-scrolling container is what `position: sticky` pins against.
    const scroller = document.createElement('div');
    scroller.style.height = '250px';
    scroller.style.overflowY = 'auto';
    scroller.appendChild(fixture.nativeElement);
    document.body.appendChild(scroller);

    try {
      const header = scroller.querySelector('.page-header') as HTMLElement;
      const firstCard = scroller.querySelectorAll('.page-card')[1] as HTMLElement; // body card, not the toolbar

      const containerTop = scroller.getBoundingClientRect().top;
      const cardTopBefore = firstCard.getBoundingClientRect().top;

      scroller.scrollTop = 400; // scroll the form body well past the toolbar's height

      const headerTop = header.getBoundingClientRect().top;
      const cardTopAfter = firstCard.getBoundingClientRect().top;

      // Body content actually moved up (the form scrolled)...
      expect(cardTopAfter).toBeLessThan(cardTopBefore - 100);
      // ...but the toolbar stayed pinned at the top of the scroll region.
      expect(Math.abs(headerTop - containerTop)).toBeLessThan(2);
    } finally {
      document.body.removeChild(scroller);
    }
  });
});

describe('CourseForm (edit mode)', () => {
  it('loads the course and patches the form', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBe(true);
    expect(component['form'].controls.title.value).toBe('PMP 專案管理');
    expect(component['form'].controls.courseId.value).toBe('PMP');
  });

  it('serializes schedule dates back to ISO on update', () => {
    const { component, serviceSpy } = setup('2');
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
    const request = serviceSpy.update.calls.mostRecent().args[0];
    expect(request.scheduleOn).toBe('2026-01-01');
    expect(request.scheduleOff).toBe('2030-12-31');
  });
});
