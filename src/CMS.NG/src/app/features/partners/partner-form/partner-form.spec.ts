import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { PartnerForm } from './partner-form';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

const existing: Partner = {
  pkid: 2,
  name: '巨匠',
  appKey: 'PCS',
  nameOnPartnerMenu: '巨匠電腦',
  nameOnCourseDetailPage: '巨匠',
  displayOrder: 2,
  imageFilename: null,
};

function setup(id: string | null) {
  const serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['getById', 'create', 'update']);
  serviceSpy.getById.and.returnValue(of(existing));
  serviceSpy.create.and.returnValue(of({ ...existing, pkid: 9 }));
  serviceSpy.update.and.returnValue(of(void 0));

  TestBed.configureTestingModule({
    imports: [PartnerForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: PartnerService, useValue: serviceSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
      },
    ],
  });

  const fixture: ComponentFixture<PartnerForm> = TestBed.createComponent(PartnerForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy };
}

describe('PartnerForm (add mode)', () => {
  it('starts empty and does not load a partner', () => {
    const { component, serviceSpy } = setup(null);
    expect(serviceSpy.getById).not.toHaveBeenCalled();
    expect(component['isEdit']()).toBe(false);
  });

  it('does not save when required fields are missing', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].controls.name.touched).toBe(true);
    expect(component['form'].controls.appKey.touched).toBe(true);
  });

  it('creates a partner and navigates using the server-assigned pkid', () => {
    const { component, serviceSpy } = setup(null);
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component['form'].patchValue({
      name: '資策會',
      appKey: 'III',
      nameOnPartnerMenu: '資訊工業策進會',
      nameOnCourseDetailPage: '資策會',
      displayOrder: 3,
    });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/partners', 9]);
  });
});

describe('PartnerForm (edit mode)', () => {
  it('loads the partner and patches the form', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBe(true);
    expect(component['form'].controls.name.value).toBe('巨匠');
    expect(component['form'].controls.appKey.value).toBe('PCS');
  });

  it('updates the partner on save and navigates to the detail page', () => {
    const { component, serviceSpy } = setup('2');
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/partners', 2]);
  });
});
