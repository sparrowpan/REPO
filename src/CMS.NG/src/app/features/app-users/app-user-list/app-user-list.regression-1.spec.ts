import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { AppUserList } from './app-user-list';
import { AppUserService } from '@core/services/app-user.service';
import { AppUser } from '@core/models/app-user.model';

// Regression: ISSUE-001 — AppUser password time rendered 8 hours in the future
// Found by /qa on 2026-07-17
// Report: .gstack/qa-reports/qa-report-localhost-4200-2026-07-17.md
//
// PasswordUpdatedTime is stamped with DateTime.Now / GETDATE() and serialized with no
// timezone designator, so it is LOCAL time. The templates appended 'Z' before the date
// pipe, which marked that local value as UTC and re-converted it — a reset at 13:44
// rendered as 21:44 for UTC+8 users.
//
// These assert the local wall clock of the input survives rendering. An unmarked
// date-time string always parses as local (ES spec), so the expectation holds in any
// timezone. Note the defect is unobservable where the offset is zero — in UTC the old
// 'Z' form produced identical output — so these discriminate anywhere offset != 0.
describe('AppUserList — password time rendering (ISSUE-001)', () => {
  let fixture: ComponentFixture<AppUserList>;
  let serviceSpy: jasmine.SpyObj<AppUserService>;

  const PASSWORD_TIME_CELL = 5;

  function renderWith(passwordUpdatedTime: string | null): string {
    const users: AppUser[] = [
      { pkid: 1, userId: 'helen', userName: 'Helen Wang', isActive: true, passwordUpdatedTime, roleCount: 0, roleIds: [] },
    ];
    serviceSpy.query.and.returnValue(of(users));
    fixture = TestBed.createComponent(AppUserList);
    fixture.detectChanges();
    const row = (fixture.nativeElement as HTMLElement).querySelector('tbody tr');
    return row!.children[PASSWORD_TIME_CELL].textContent!.trim();
  }

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['query', 'delete']);
    serviceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [AppUserList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AppUserService, useValue: serviceSpy },
      ],
    }).compileComponents();
  });

  it('renders the stamped local time unshifted', () => {
    expect(renderWith('2026-07-17T13:44:13.29')).toBe('2026-07-17 13:44');
  });

  it('does not roll an evening stamp onto the next day', () => {
    // The pre-fix form pushed 22:30 past midnight for any positive offset.
    expect(renderWith('2026-07-15T22:30:00')).toBe('2026-07-15 22:30');
  });

  it('renders a dash when the user has never set a password', () => {
    expect(renderWith(null)).toBe('—');
  });
});
