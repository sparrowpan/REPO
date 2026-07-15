import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { AppUserForm } from './app-user-form';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';

const roles: AppRoleLookup[] = [
  { roleId: 'Admin', roleName: 'Administrator', label: 'Administrator (Admin)' },
  { roleId: 'User', roleName: 'User', label: 'User (User)' },
];

const helen: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Wang',
  isActive: true,
  passwordUpdatedTime: '2026-01-01T00:00:00',
  roleCount: 1,
  roleIds: ['Admin'],
};

function setup(userId: string | null) {
  const serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['getById', 'create', 'update']);
  serviceSpy.getById.and.returnValue(of(helen));
  serviceSpy.create.and.returnValue(of(helen));
  serviceSpy.update.and.returnValue(of(void 0));

  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppRoles']);
  lookupSpy.getAppRoles.and.returnValue(of(roles));

  TestBed.configureTestingModule({
    imports: [AppUserForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: AppUserService, useValue: serviceSpy },
      { provide: LookupService, useValue: lookupSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(userId ? { id: userId } : {}) } },
      },
    ],
  });

  const fixture: ComponentFixture<AppUserForm> = TestBed.createComponent(AppUserForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, lookupSpy };
}

describe('AppUserForm (add mode)', () => {
  it('loads roles and defaults isActive to true with userId enabled', () => {
    const { component, lookupSpy, serviceSpy } = setup(null);
    expect(lookupSpy.getAppRoles).toHaveBeenCalled();
    expect(serviceSpy.getById).not.toHaveBeenCalled();
    expect(component['isEdit']()).toBe(false);
    expect(component['form'].controls.isActive.value).toBe(true);
    expect(component['form'].controls.userId.disabled).toBe(false);
  });

  it('does not save when required fields are missing', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].controls.userId.touched).toBe(true);
  });

  it('creates a user when the form is valid', () => {
    const { component, serviceSpy } = setup(null);
    component['form'].patchValue({ userId: 'jenny', userName: 'Jenny' });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
    expect(serviceSpy.create.calls.mostRecent().args[0].userId).toBe('jenny');
  });

  it('shows a conflict message on 409', () => {
    const { component, serviceSpy } = setup(null);
    serviceSpy.create.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { message: '使用者代碼已存在。' } })),
    );
    component['form'].patchValue({ userId: 'helen', userName: 'Dup' });
    component.save();
    expect(component['saving']()).toBe(false);
  });
});

describe('AppUserForm (edit mode)', () => {
  it('loads the user, patches the form, and disables userId', () => {
    const { component, serviceSpy } = setup('helen');
    expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
    expect(component['isEdit']()).toBe(true);
    expect(component['form'].controls.userName.value).toBe('Helen Wang');
    expect(component['form'].controls.userId.disabled).toBe(true);
    expect(component['form'].controls.roleIds.value).toEqual(['Admin']);
  });

  it('updates the user on save and navigates to the detail page', () => {
    const { component, serviceSpy } = setup('helen');
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/app-users', 'helen']);
  });
});
