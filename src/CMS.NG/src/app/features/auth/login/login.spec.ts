import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { environment } from '@env/environment';
import { Login } from './login';

describe('Login', () => {
  let httpMock: HttpTestingController;
  let router: { navigate: jasmine.Spy };
  const loginUrl = `${environment.apiBaseUrl}/Auth/login`;

  beforeEach(async () => {
    sessionStorage.clear();
    router = { navigate: jasmine.createSpy('navigate') };
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  function componentOf() {
    const fixture = TestBed.createComponent(Login);
    return fixture.componentInstance as unknown as {
      form: { setValue: (v: { userId: string; password: string }) => void };
      submit: () => void;
      error: () => string | null;
    };
  }

  it('creates', () => {
    expect(componentOf()).toBeTruthy();
  });

  it('posts credentials and navigates home on success', () => {
    const cmp = componentOf();
    cmp.form.setValue({ userId: 'helen', password: 'secret123' });
    cmp.submit();

    const req = httpMock.expectOne(loginUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'helen', password: 'secret123' });
    req.flush({ userId: 'helen', userName: 'Helen Wang', accessToken: 'h.e.n' });

    expect(router.navigate).toHaveBeenCalledWith(['/']);
    expect(sessionStorage.getItem('cms-auth')).not.toBeNull();
  });

  it('shows an error message on a 401', () => {
    const cmp = componentOf();
    cmp.form.setValue({ userId: 'helen', password: 'wrong' });
    cmp.submit();

    httpMock
      .expectOne(loginUrl)
      .flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(cmp.error()).toContain('帳號或密碼錯誤');
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
