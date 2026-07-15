import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { CourseGroupDetail } from './course-group-detail';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const courseGroup: CourseGroup = {
  pkid: 1,
  description: '資訊技術',
};

describe('CourseGroupDetail', () => {
  let fixture: ComponentFixture<CourseGroupDetail>;
  let component: CourseGroupDetail;
  let serviceSpy: jasmine.SpyObj<CourseGroupService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', ['getById']);
    serviceSpy.getById.and.returnValue(of(courseGroup));

    await TestBed.configureTestingModule({
      imports: [CourseGroupDetail],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CourseGroupService, useValue: serviceSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseGroupDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the course group by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith(1);
    expect(component['courseGroup']()?.description).toBe('資訊技術');
  });

  it('renders the course group fields', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('資訊技術');
  });

  it('edit navigates to the edit route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/course-groups', 1, 'edit']);
  });
});
