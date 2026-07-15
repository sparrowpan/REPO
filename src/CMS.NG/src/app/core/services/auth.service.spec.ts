import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { environment } from '@env/environment';
import { AuthService } from './auth.service';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/** Build an unsigned JWT (header.payload.sig) with the given payload — enough for client-side decoding. */
function makeJwt(payload: Record<string, unknown>): string {
  const enc = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${enc({ alg: 'HS256', typ: 'JWT' })}.${enc(payload)}.sig`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/Auth`;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('login() POSTs credentials and stores the profile in session storage', () => {
    const token = makeJwt({ userId: 'helen', userName: 'Helen Wang', [ROLE_CLAIM]: ['Admin', 'User'] });
    const profile = { userId: 'helen', userName: 'Helen Wang', accessToken: token };

    service.login({ userId: 'helen', password: 'secret123' }).subscribe();

    const req = httpMock.expectOne(`${base}/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'helen', password: 'secret123' });
    req.flush(profile);

    expect(JSON.parse(sessionStorage.getItem('cms-auth')!)).toEqual(profile);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.userName()).toBe('Helen Wang');
    expect(service.token).toBe(token);
  });

  it('roles() decodes role claims from the token', () => {
    const token = makeJwt({ [ROLE_CLAIM]: ['Admin', 'User'] });

    service.login({ userId: 'x', password: 'y' }).subscribe();
    httpMock.expectOne(`${base}/login`).flush({ userId: 'x', userName: 'X', accessToken: token });

    expect(service.roles()).toEqual(['Admin', 'User']);
    expect(service.hasRole('Admin')).toBe(true);
    expect(service.hasRole('Editor')).toBe(false);
  });

  it('normalizes a single (non-array) role claim to an array', () => {
    const token = makeJwt({ [ROLE_CLAIM]: 'Admin' });

    service.login({ userId: 'x', password: 'y' }).subscribe();
    httpMock.expectOne(`${base}/login`).flush({ userId: 'x', userName: 'X', accessToken: token });

    expect(service.roles()).toEqual(['Admin']);
  });

  it('logout() clears session storage and the profile', () => {
    service.login({ userId: 'x', password: 'y' }).subscribe();
    httpMock.expectOne(`${base}/login`).flush({ userId: 'x', userName: 'X', accessToken: makeJwt({}) });
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(sessionStorage.getItem('cms-auth')).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.roles()).toEqual([]);
  });
});
