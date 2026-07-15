import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { ConfirmationService } from 'primeng/api';

import { AppUserDetail } from './app-user-detail';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const STORAGE_KEY = 'cms-auth';

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

const user: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Wang',
  isActive: true,
  passwordUpdatedTime: '2026-01-01T00:00:00',
  roleCount: 2,
  roleIds: ['Admin', 'User'],
};

/** The reset-password button's Chinese label, used to locate it in the rendered toolbar. */
const RESET_LABEL = '重設密碼';

function setup(signedInRoles: string[]) {
  sessionStorage.clear();
  signIn(signedInRoles);

  const serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['getById', 'resetPassword']);
  serviceSpy.getById.and.returnValue(of(user));
  serviceSpy.resetPassword.and.returnValue(of(void 0));
  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppRoles']);
  lookupSpy.getAppRoles.and.returnValue(of(roles));

  TestBed.configureTestingModule({
    imports: [AppUserDetail],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: AppUserService, useValue: serviceSpy },
      { provide: LookupService, useValue: lookupSpy },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'helen' }) } } },
    ],
  });

  const fixture: ComponentFixture<AppUserDetail> = TestBed.createComponent(AppUserDetail);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy };
}

describe('AppUserDetail', () => {
  afterEach(() => sessionStorage.clear());

  it('loads the user by id from the route', () => {
    const { component, serviceSpy } = setup(['Admin']);
    expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
    expect(component['user']()?.userName).toBe('Helen Wang');
  });

  it('resolves role ids to their display labels', () => {
    const { component } = setup(['Admin']);
    expect(component['roleLabels']()).toEqual(['Administrator (Admin)', 'User (User)']);
  });

  it('renders the user fields', () => {
    const { fixture } = setup(['Admin']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Helen Wang');
  });

  it('edit navigates to the edit route', () => {
    const { component } = setup(['Admin']);
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/app-users', 'helen', 'edit']);
  });

  // --- Reset password button (Admin-only) -------------------------------

  it('shows the reset-password button for an Admin', () => {
    const { component, fixture } = setup(['Admin']);
    expect(component['isAdmin']()).toBe(true);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain(RESET_LABEL);
  });

  it('hides the reset-password button for a non-Admin', () => {
    const { component, fixture } = setup(['User']);
    expect(component['isAdmin']()).toBe(false);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain(RESET_LABEL);
  });

  it('reset password (confirmed) calls the service for an Admin', () => {
    const { component, fixture, serviceSpy } = setup(['Admin']);
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmResetPassword();
    expect(serviceSpy.resetPassword).toHaveBeenCalledWith('helen');
  });

  it('does not reset for a non-Admin even if invoked directly', () => {
    const { component, fixture, serviceSpy } = setup(['User']);
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    const confirmSpy = spyOn(confirmation, 'confirm').and.callThrough();
    component.confirmResetPassword();
    expect(confirmSpy).not.toHaveBeenCalled();
    expect(serviceSpy.resetPassword).not.toHaveBeenCalled();
  });
});
