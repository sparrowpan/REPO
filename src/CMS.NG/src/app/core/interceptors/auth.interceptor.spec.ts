import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { environment } from '@env/environment';
import { authInterceptor, FALLBACK_ERROR_MESSAGE } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  let messages: jasmine.SpyObj<MessageService>;
  const url = `${environment.apiBaseUrl}/appusers`;
  const profile = { userId: 'helen', userName: 'Helen', accessToken: 'tok-123' };
  const safeMessage = '系統發生錯誤，請稍後再試。An unexpected error occurred.';

  beforeEach(() => {
    sessionStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };
    messages = jasmine.createSpyObj<MessageService>('MessageService', ['add']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: MessageService, useValue: messages },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  // --- Bearer token -------------------------------------------------------

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

  // --- 401: session handling is unchanged ---------------------------------

  it('clears session storage and redirects to /login on a 401', () => {
    sessionStorage.setItem('cms-auth', JSON.stringify(profile));

    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    const req = httpMock.expectOne(url);
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem('cms-auth')).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('does not toast on a 401 — the redirect to login is the feedback', () => {
    sessionStorage.setItem('cms-auth', JSON.stringify(profile));

    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    httpMock.expectOne(url).flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(messages.add).not.toHaveBeenCalled();
  });

  // --- 5xx: friendly toast ------------------------------------------------

  it('shows an error toast carrying the safe message on a 500', () => {
    let caught: unknown = null;
    http.get(url).subscribe({ next: () => undefined, error: (err) => (caught = err) });

    httpMock
      .expectOne(url)
      .flush({ message: safeMessage, traceId: '00-abc-def-01' }, { status: 500, statusText: 'Server Error' });

    expect(messages.add).toHaveBeenCalledTimes(1);
    expect(messages.add).toHaveBeenCalledWith(
      jasmine.objectContaining({ severity: 'error', detail: safeMessage }),
    );

    // The session survives and the user stays put — a 500 is not an auth failure.
    expect(router.navigate).not.toHaveBeenCalled();
    // The error still reaches the caller so components can react.
    expect(caught).toBeTruthy();
  });

  it('toasts on other 5xx statuses too', () => {
    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    httpMock
      .expectOne(url)
      .flush({ message: safeMessage }, { status: 503, statusText: 'Service Unavailable' });

    expect(messages.add).toHaveBeenCalledWith(jasmine.objectContaining({ detail: safeMessage }));
  });

  it('falls back to a friendly message when a 5xx body carries none', () => {
    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    httpMock.expectOne(url).flush(null, { status: 500, statusText: 'Server Error' });

    expect(messages.add).toHaveBeenCalledWith(
      jasmine.objectContaining({ detail: FALLBACK_ERROR_MESSAGE }),
    );
  });

  it('never renders a non-JSON 5xx body, such as a proxy HTML error page', () => {
    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    httpMock
      .expectOne(url)
      .flush('<html><body>Stack trace: at Foo.Bar()</body></html>', {
        status: 502,
        statusText: 'Bad Gateway',
      });

    expect(messages.add).toHaveBeenCalledWith(
      jasmine.objectContaining({ detail: FALLBACK_ERROR_MESSAGE }),
    );
  });

  // --- Everything else is left to the components --------------------------

  it('does not toast on a 400 — validation errors surface on the form', () => {
    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    httpMock
      .expectOne(url)
      .flush({ errors: { Description: ['required'] } }, { status: 400, statusText: 'Bad Request' });

    expect(messages.add).not.toHaveBeenCalled();
  });

  it('does not toast on a 409 — the component reports the conflict', () => {
    http.get(url).subscribe({ next: () => undefined, error: () => undefined });

    httpMock
      .expectOne(url)
      .flush({ message: '使用者代碼已存在。' }, { status: 409, statusText: 'Conflict' });

    expect(messages.add).not.toHaveBeenCalled();
  });
});
