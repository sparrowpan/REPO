import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
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
        { provide: CourseService, useValue: serviceSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

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
});
