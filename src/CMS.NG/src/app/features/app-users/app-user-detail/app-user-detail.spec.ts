import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ConfirmationService } from 'primeng/api';

import { AppUserDetail } from './app-user-detail';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { AppRoleLookup } from '@core/models/app-role-lookup.model';

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

describe('AppUserDetail', () => {
  let fixture: ComponentFixture<AppUserDetail>;
  let component: AppUserDetail;
  let serviceSpy: jasmine.SpyObj<AppUserService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['getById', 'resetPassword']);
    serviceSpy.getById.and.returnValue(of(user));
    serviceSpy.resetPassword.and.returnValue(of(void 0));
    const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppRoles']);
    lookupSpy.getAppRoles.and.returnValue(of(roles));

    await TestBed.configureTestingModule({
      imports: [AppUserDetail],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AppUserService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'helen' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppUserDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the user by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
    expect(component['user']()?.userName).toBe('Helen Wang');
  });

  it('resolves role ids to their display labels', () => {
    expect(component['roleLabels']()).toEqual(['Administrator (Admin)', 'User (User)']);
  });

  it('renders the user fields', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Helen Wang');
  });

  it('reset password (confirmed) calls the service', () => {
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmResetPassword();
    expect(serviceSpy.resetPassword).toHaveBeenCalledWith('helen');
  });

  it('edit navigates to the edit route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/app-users', 'helen', 'edit']);
  });
});
