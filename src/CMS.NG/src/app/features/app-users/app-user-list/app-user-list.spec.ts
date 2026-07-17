import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AppUserList } from './app-user-list';
import { AppUserService } from '@core/services/app-user.service';
import { AppUser } from '@core/models/app-user.model';

describe('AppUserList', () => {
  let fixture: ComponentFixture<AppUserList>;
  let component: AppUserList;
  let serviceSpy: jasmine.SpyObj<AppUserService>;

  const users: AppUser[] = [
    { pkid: 1, userId: 'helen', userName: 'Helen Wang', isActive: true, passwordUpdatedTime: null, roleCount: 2, roleIds: [] },
    { pkid: 2, userId: 'miles', userName: 'Miles Sun', isActive: false, passwordUpdatedTime: null, roleCount: 1, roleIds: [] },
  ];

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(users));
    serviceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [AppUserList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AppUserService, useValue: serviceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppUserList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads users on init via query()', () => {
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['users']().length).toBe(2);
  });

  it('renders a row per user', () => {
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilters persists filters to sessionStorage and reloads', () => {
    component['filters'] = { keyword: 'helen', isActive: null };
    component.applyFilters();
    expect(sessionStorage.getItem('app-user-list-filters')).toContain('helen');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('clearFilters resets filters and removes the stored key', () => {
    sessionStorage.setItem('app-user-list-filters', '{"keyword":"x"}');
    component.clearFilters();
    expect(sessionStorage.getItem('app-user-list-filters')).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.view(users[0]);
    expect(navSpy).toHaveBeenCalledWith(['/app-users', 'helen']);
  });

  it('confirmDelete accepting deletes the user and reloads', () => {
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmDelete(users[1]);
    expect(serviceSpy.delete).toHaveBeenCalledWith('miles');
  });

  it('surfaces a load error without throwing', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBe(false);
  });
});
