import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ConfirmationService } from 'primeng/api';

import { AppUserForm } from './app-user-form';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const STORAGE_KEY = 'cms-auth';

/** Build an unsigned-but-well-formed JWT so AuthService can decode role claims from it. */
function makeJwt(payload: Record<string, unknown>): string {
  const enc = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${enc({ alg: 'HS256', typ: 'JWT' })}.${enc(payload)}.sig`;
}

/** Seed session storage with a signed-in profile carrying the given roles (empty = signed out). */
function signIn(roles: string[]): void {
  if (roles.length === 0) return;
  const token = makeJwt({ userId: 'boss', userName: 'Boss', [ROLE_CLAIM]: roles });
  sessionStorage.setItem(
    STORAGE_KEY,
    JSON.stringify({ userId: 'boss', userName: 'Boss', accessToken: token }),
  );
}

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

function setup(userId: string | null, signedInRoles: string[] = []) {
  sessionStorage.clear();
  signIn(signedInRoles);

  const serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', [
    'getById',
    'create',
    'update',
    'resetPassword',
  ]);
  serviceSpy.getById.and.returnValue(of(helen));
  serviceSpy.create.and.returnValue(of(helen));
  serviceSpy.update.and.returnValue(of(void 0));
  serviceSpy.resetPassword.and.returnValue(of(void 0));

  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppRoles']);
  lookupSpy.getAppRoles.and.returnValue(of(roles));

  TestBed.configureTestingModule({
    imports: [AppUserForm],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
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

afterEach(() => sessionStorage.clear());

/** The reset-password button's Chinese label, used to locate it in the rendered toolbar. */
const RESET_LABEL = '重設密碼為預設值';

function resetButtonText(fixture: ComponentFixture<AppUserForm>): string {
  return (fixture.nativeElement as HTMLElement).textContent ?? '';
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

  it('never shows the reset-password button in add mode, even for an Admin', () => {
    const { fixture } = setup(null, ['Admin']);
    expect(resetButtonText(fixture)).not.toContain(RESET_LABEL);
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

  // --- Reset password button (Admin-only) -------------------------------

  it('shows the reset-password button for an Admin', () => {
    const { component, fixture } = setup('helen', ['Admin']);
    expect(component['isAdmin']()).toBe(true);
    expect(resetButtonText(fixture)).toContain(RESET_LABEL);
  });

  it('hides the reset-password button for a non-Admin', () => {
    const { component, fixture } = setup('helen', ['User']);
    expect(component['isAdmin']()).toBe(false);
    expect(resetButtonText(fixture)).not.toContain(RESET_LABEL);
  });

  it('hides the reset-password button when signed out', () => {
    const { component, fixture } = setup('helen', []);
    expect(component['isAdmin']()).toBe(false);
    expect(resetButtonText(fixture)).not.toContain(RESET_LABEL);
  });

  it('reset (confirmed) calls the service with the edited userId', () => {
    const { component, fixture, serviceSpy } = setup('helen', ['Admin']);
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmResetPassword();
    expect(serviceSpy.resetPassword).toHaveBeenCalledWith('helen');
  });

  it('does not reset for a non-Admin even if invoked directly', () => {
    const { component, fixture, serviceSpy } = setup('helen', ['User']);
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    const confirmSpy = spyOn(confirmation, 'confirm').and.callThrough();
    component.confirmResetPassword();
    expect(confirmSpy).not.toHaveBeenCalled();
    expect(serviceSpy.resetPassword).not.toHaveBeenCalled();
  });
});
