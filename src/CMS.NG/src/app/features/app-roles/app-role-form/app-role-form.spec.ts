import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { AppRoleForm } from './app-role-form';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { AppRole } from '@core/models/app-role.model';
import { AppUserLookup } from '@core/models/app-user-lookup.model';

const users: AppUserLookup[] = [
  { userId: 'helen', userName: 'helen', label: 'helen (helen)' },
  { userId: 'miles', userName: 'Miles Sun', label: 'Miles Sun (miles)' },
];

const adminRole: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 1,
  userIds: ['helen'],
};

function setup(roleId: string | null) {
  const serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['getById', 'create', 'update']);
  serviceSpy.getById.and.returnValue(of(adminRole));
  serviceSpy.create.and.returnValue(of(adminRole));
  serviceSpy.update.and.returnValue(of(void 0));

  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppUsers']);
  lookupSpy.getAppUsers.and.returnValue(of(users));

  TestBed.configureTestingModule({
    imports: [AppRoleForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: AppRoleService, useValue: serviceSpy },
      { provide: LookupService, useValue: lookupSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(roleId ? { id: roleId } : {}) } },
      },
    ],
  });

  const fixture: ComponentFixture<AppRoleForm> = TestBed.createComponent(AppRoleForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, lookupSpy };
}

describe('AppRoleForm (add mode)', () => {
  it('loads users and defaults permissionLevel to 100 with roleId enabled', () => {
    const { component, lookupSpy, serviceSpy } = setup(null);
    expect(lookupSpy.getAppUsers).toHaveBeenCalled();
    expect(serviceSpy.getById).not.toHaveBeenCalled();
    expect(component['isEdit']()).toBe(false);
    expect(component['form'].controls.permissionLevel.value).toBe(100);
    expect(component['form'].controls.roleId.disabled).toBe(false);
  });

  it('does not save when required fields are missing', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(component['form'].controls.roleId.touched).toBe(true);
  });

  it('creates a role when the form is valid', () => {
    const { component, serviceSpy } = setup(null);
    component['form'].patchValue({ roleId: 'Editor', roleName: 'Editor', permissionLevel: 50 });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
    expect(serviceSpy.create.calls.mostRecent().args[0].roleId).toBe('Editor');
  });

  it('shows a conflict message on 409', () => {
    const { component, serviceSpy } = setup(null);
    serviceSpy.create.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { message: '角色代碼已存在。' } })),
    );
    component['form'].patchValue({ roleId: 'Admin', roleName: 'Dup', permissionLevel: 1 });
    component.save();
    expect(component['saving']()).toBe(false);
  });
});

describe('AppRoleForm (edit mode)', () => {
  it('loads the role, patches the form, and disables roleId', () => {
    const { component, serviceSpy } = setup('Admin');
    expect(serviceSpy.getById).toHaveBeenCalledWith('Admin');
    expect(component['isEdit']()).toBe(true);
    expect(component['form'].controls.roleName.value).toBe('Administrator');
    expect(component['form'].controls.roleId.disabled).toBe(true);
    expect(component['form'].controls.userIds.value).toEqual(['helen']);
  });

  it('updates the role on save and navigates to the detail page', () => {
    const { component, serviceSpy } = setup('Admin');
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/app-roles', 'Admin']);
  });
});
