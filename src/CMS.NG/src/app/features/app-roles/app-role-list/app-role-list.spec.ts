import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AppRoleList } from './app-role-list';
import { AppRoleService } from '@core/services/app-role.service';
import { AppRole } from '@core/models/app-role.model';

describe('AppRoleList', () => {
  let fixture: ComponentFixture<AppRoleList>;
  let component: AppRoleList;
  let serviceSpy: jasmine.SpyObj<AppRoleService>;

  const roles: AppRole[] = [
    { pkid: 1, roleId: 'Admin', roleName: 'Administrator', permissionLevel: 1, description: '系統管理員', userCount: 3, userIds: [] },
    { pkid: 2, roleId: 'User', roleName: 'User', permissionLevel: 100, description: '一般使用者', userCount: 9, userIds: [] },
  ];

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(roles));
    serviceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [AppRoleList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AppRoleService, useValue: serviceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppRoleList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads roles on init via query()', () => {
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['roles']().length).toBe(2);
  });

  it('renders a row per role', () => {
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilters persists filters to sessionStorage and reloads', () => {
    component['filters'] = { keyword: 'admin', permissionLevel: null };
    component.applyFilters();
    expect(sessionStorage.getItem('app-role-list-filters')).toContain('admin');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('clearFilters resets filters and removes the stored key', () => {
    sessionStorage.setItem('app-role-list-filters', '{"keyword":"x"}');
    component.clearFilters();
    expect(sessionStorage.getItem('app-role-list-filters')).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.view(roles[0]);
    expect(navSpy).toHaveBeenCalledWith(['/app-roles', 'Admin']);
  });

  it('confirmDelete accepting deletes the role and reloads', () => {
    // ConfirmationService is provided at the component level, so resolve it from
    // the component's own injector rather than the TestBed root.
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmDelete(roles[1]);
    expect(serviceSpy.delete).toHaveBeenCalledWith('User');
  });

  it('surfaces a load error without throwing', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBe(false);
  });
});
