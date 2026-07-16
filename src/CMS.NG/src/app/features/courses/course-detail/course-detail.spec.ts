import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Title } from '@angular/platform-browser';
import { of } from 'rxjs';

import { CourseDetail } from './course-detail';
import { CourseService } from '@core/services/course.service';
import { Course } from '@core/models/course.model';

const course = {
  pkid: 1,
  title: 'AZ-900 基礎課程',
  courseId: 'AZ900',
  partnerPkid: 1,
  partner: { pkid: 1, name: '微軟' },
  courseGroup: null,
  publishStatus: { pkid: 20, description: '已上架' },
  publishStatusPkid: 20,
  scheduleOn: '2026-01-01',
  scheduleOff: '2030-12-31',
  jobCategories: [{ pkid: 1, description: '工程師' }],
  certifications: [],
  jobCategoryPkids: [1],
  certificationPkids: [],
} as unknown as Course;

describe('CourseDetail', () => {
  let fixture: ComponentFixture<CourseDetail>;
  let component: CourseDetail;
  let serviceSpy: jasmine.SpyObj<CourseService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['getById']);
    serviceSpy.getById.and.returnValue(of(course));

    await TestBed.configureTestingModule({
      imports: [CourseDetail],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: CourseService, useValue: serviceSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('loads the course by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith(1);
    expect(component['course']()?.title).toBe('AZ-900 基礎課程');
  });

  it('renders the course fields', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('AZ-900 基礎課程');
    expect(text).toContain('微軟');
  });

  it('edit navigates to the edit route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/courses', 1, 'edit']);
  });

  describe('課程簡介 brochure', () => {
    function toggle(): void {
      component.toggleBrochure();
      fixture.detectChanges();
    }

    it('is absent until the rep asks for it', () => {
      expect(element().querySelector('course-brochure-print')).toBeNull();
    });

    it('replaces the admin cards when the preview opens', () => {
      toggle();

      expect(element().querySelector('course-brochure-print')).not.toBeNull();
      // Not merely hidden — gone. A print rule can regress silently; absence cannot.
      expect(element().textContent).not.toContain('主代碼');
      expect(element().textContent).not.toContain('網址代稱');
    });

    it('restores the admin cards when the preview closes', () => {
      toggle();
      toggle();

      expect(element().querySelector('course-brochure-print')).toBeNull();
      expect(element().textContent).toContain('主代碼');
    });

    // T4
    it('列印 calls window.print()', () => {
      const printSpy = spyOn(window, 'print');
      toggle();

      element()
        .querySelectorAll('button')
        .forEach((b) => {
          if (b.textContent?.includes('列印')) b.click();
        });

      expect(printSpy).toHaveBeenCalled();
    });

    // T5 — Chrome's Save-as-PDF filename is document.title
    it('names the document for the Save-as-PDF filename', () => {
      spyOn(window, 'print');
      component.printBrochure();

      expect(TestBed.inject(Title).getTitle()).toBe('課程簡介_AZ-900 基礎課程');
    });

    it('restores the original title on destroy, so it cannot leak to other routes', () => {
      const title = TestBed.inject(Title);
      const original = title.getTitle();
      spyOn(window, 'print');

      component.printBrochure();
      expect(title.getTitle()).toBe('課程簡介_AZ-900 基礎課程');

      fixture.destroy();
      expect(title.getTitle()).toBe(original);
    });
  });
});
