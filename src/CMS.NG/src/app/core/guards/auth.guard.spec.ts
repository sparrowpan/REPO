import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let router: { createUrlTree: jasmine.Spy };
  const urlTree = {} as UrlTree;

  beforeEach(() => {
    sessionStorage.clear();
    router = { createUrlTree: jasmine.createSpy('createUrlTree').and.returnValue(urlTree) };
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), { provide: Router, useValue: router }],
    });
  });

  afterEach(() => sessionStorage.clear());

  function runGuard() {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }

  it('redirects to /login when there is no token', () => {
    const result = runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
    expect(result).toBe(urlTree);
  });

  it('allows activation when a token is present in session storage', () => {
    sessionStorage.setItem(
      'cms-auth',
      JSON.stringify({ userId: 'helen', userName: 'Helen', accessToken: 'tok-123' }),
    );

    const result = runGuard();

    expect(result).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });
});
