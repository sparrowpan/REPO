import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { AppRoleDetail } from './app-role-detail';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { AppRole } from '@core/models/app-role.model';
import { AppUserLookup } from '@core/models/app-user-lookup.model';

const users: AppUserLookup[] = [
  { userId: 'helen', userName: 'helen', label: 'helen (helen)' },
  { userId: 'miles', userName: 'Miles Sun', label: 'Miles Sun (miles)' },
];

const role: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 2,
  userIds: ['helen', 'miles'],
};

describe('AppRoleDetail', () => {
  let fixture: ComponentFixture<AppRoleDetail>;
  let component: AppRoleDetail;
  let serviceSpy: jasmine.SpyObj<AppRoleService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['getById']);
    serviceSpy.getById.and.returnValue(of(role));
    const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppUsers']);
    lookupSpy.getAppUsers.and.returnValue(of(users));

    await TestBed.configureTestingModule({
      imports: [AppRoleDetail],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AppRoleService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'Admin' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppRoleDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the role by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith('Admin');
    expect(component['role']()?.roleName).toBe('Administrator');
  });

  it('resolves user ids to their display labels', () => {
    expect(component['userLabels']()).toEqual(['helen (helen)', 'Miles Sun (miles)']);
  });

  it('renders the role fields', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Administrator');
    expect(text).toContain('系統管理員');
  });

  it('edit navigates to the edit route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/app-roles', 'Admin', 'edit']);
  });
});
