import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { CourseGroupForm } from './course-group-form';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const existing: CourseGroup = {
  pkid: 2,
  description: '商業管理',
};

function setup(id: string | null) {
  const serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', ['getById', 'create', 'update']);
  serviceSpy.getById.and.returnValue(of(existing));
  serviceSpy.create.and.returnValue(of({ ...existing, pkid: 9 }));
  serviceSpy.update.and.returnValue(of(void 0));

  TestBed.configureTestingModule({
    imports: [CourseGroupForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: CourseGroupService, useValue: serviceSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
      },
    ],
  });

  const fixture: ComponentFixture<CourseGroupForm> = TestBed.createComponent(CourseGroupForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy };
}

describe('CourseGroupForm (add mode)', () => {
  it('starts empty and does not load a course group', () => {
    const { component, serviceSpy } = setup(null);
    expect(serviceSpy.getById).not.toHaveBeenCalled();
    expect(component['isEdit']()).toBe(false);
  });

  it('does not save when required fields are missing', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].controls.description.touched).toBe(true);
  });

  it('creates a course group and navigates using the server-assigned pkid', () => {
    const { component, serviceSpy } = setup(null);
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component['form'].patchValue({ description: '設計創意' });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/course-groups', 9]);
  });
});

describe('CourseGroupForm (edit mode)', () => {
  it('loads the course group and patches the form', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBe(true);
    expect(component['form'].controls.description.value).toBe('商業管理');
  });

  it('updates the course group on save and navigates to the detail page', () => {
    const { component, serviceSpy } = setup('2');
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/course-groups', 2]);
  });
});
