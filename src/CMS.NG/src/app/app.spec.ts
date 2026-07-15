import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { routes } from './app.routes';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function makeJwt(payload: Record<string, unknown>): string {
  const enc = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${enc({ alg: 'HS256', typ: 'JWT' })}.${enc(payload)}.sig`;
}

/** Seed session storage with a signed-in profile carrying the given roles. */
function signIn(roles: string[]): void {
  const token = makeJwt({ userId: 'helen', userName: 'Helen Wang', [ROLE_CLAIM]: roles });
  sessionStorage.setItem(
    'cms-auth',
    JSON.stringify({ userId: 'helen', userName: 'Helen Wang', accessToken: token }),
  );
}

describe('App', () => {
  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  afterEach(() => sessionStorage.clear());

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the UWA brand when signed in', () => {
    signIn(['Admin']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.topbar__brand-text')?.textContent).toContain('UWA');
  });

  it('shows the signed-in user name in the header', () => {
    signIn(['Admin']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.topbar__user-name')?.textContent).toContain('Helen Wang');
  });

  it('shows the 系統管理 Admin group when roles include Admin', () => {
    signIn(['Admin', 'User']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('系統管理 Admin');
    expect(text).toContain('角色 AppRole');
  });

  it('hides the 系統管理 Admin group when roles do not include Admin', () => {
    signIn(['User']);
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const text = compiled.textContent ?? '';
    expect(text).not.toContain('系統管理 Admin');
    // Still authenticated, so the shell (brand) still renders.
    expect(compiled.querySelector('.topbar__brand-text')).toBeTruthy();
  });

  it('does not render the app shell when signed out', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.topbar')).toBeNull();
  });

  it('should toggle sidebar collapse', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance as unknown as {
      collapsed: () => boolean;
      toggleCollapse: () => void;
    };
    expect(app.collapsed()).toBe(false);
    app.toggleCollapse();
    expect(app.collapsed()).toBe(true);
  });
});
