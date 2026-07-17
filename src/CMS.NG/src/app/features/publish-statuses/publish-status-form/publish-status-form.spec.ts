import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { PublishStatusForm } from './publish-status-form';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const existing: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

function setup(id: string | null) {
  const serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', [
    'getById',
    'create',
    'update',
  ]);
  serviceSpy.getById.and.returnValue(of(existing));
  serviceSpy.create.and.returnValue(of(existing));
  serviceSpy.update.and.returnValue(of(void 0));

  TestBed.configureTestingModule({
    imports: [PublishStatusForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: PublishStatusService, useValue: serviceSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
      },
    ],
  });

  const fixture: ComponentFixture<PublishStatusForm> = TestBed.createComponent(PublishStatusForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy };
}

describe('PublishStatusForm (add mode)', () => {
  it('starts empty with pkid enabled', () => {
    const { component, serviceSpy } = setup(null);
    expect(serviceSpy.getById).not.toHaveBeenCalled();
    expect(component['isEdit']()).toBe(false);
    expect(component['form'].controls.pkid.disabled).toBe(false);
  });

  it('does not save when required fields are missing', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].controls.pkid.touched).toBe(true);
    expect(component['form'].controls.description.touched).toBe(true);
  });

  it('creates a status when the form is valid', () => {
    const { component, serviceSpy } = setup(null);
    component['form'].patchValue({ pkid: 5, description: '審核中', isDraft: true });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
    expect(serviceSpy.create.calls.mostRecent().args[0].pkid).toBe(5);
  });

  it('shows a conflict message on 409', () => {
    const { component, serviceSpy } = setup(null);
    serviceSpy.create.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { message: '主代碼已存在。' } })),
    );
    component['form'].patchValue({ pkid: 1, description: 'Dup' });
    component.save();
    expect(component['saving']()).toBe(false);
  });
});

describe('PublishStatusForm (edit mode)', () => {
  it('loads the status, patches the form, and disables pkid', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBe(true);
    expect(component['form'].controls.description.value).toBe('已發布');
    expect(component['form'].controls.pkid.disabled).toBe(true);
    expect(component['form'].controls.isPublished.value).toBe(true);
  });

  it('updates the status on save and navigates to the detail page', () => {
    const { component, serviceSpy } = setup('2');
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/publish-statuses', 2]);
  });
});
