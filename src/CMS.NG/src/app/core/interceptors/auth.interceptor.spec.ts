import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { environment } from '@env/environment';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  const url = `${environment.apiBaseUrl}/appusers`;
  const profile = { userId: 'helen', userName: 'Helen', accessToken: 'tok-123' };

  beforeEach(() => {
    sessionStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('attaches the Bearer header when a token is in session storage', () => {
    sessionStorage.setItem('cms-auth', JSON.stringify(profile));

    http.get(url).subscribe();

    const req = httpMock.expectOne(url);
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-123');
    req.flush({});
  });

  it('does not attach a header when there is no token', () => {
    http.get(url).subscribe();

    const req = httpMock.expectOne(url);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('clears session storage and redirects to /login on a 401', () => {
    sessionStorage.setItem('cms-auth', JSON.stringify(profile));

    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    const req = httpMock.expectOne(url);
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem('cms-auth')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });
});
