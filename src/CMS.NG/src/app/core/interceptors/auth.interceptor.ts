import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { environment } from '@env/environment';
import { AuthService } from '@core/services/auth.service';

/**
 * Attaches `Authorization: Bearer <token>` to every outgoing API request when a token is present,
 * and, on any 401 from a protected endpoint, clears the session and redirects to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const isApiRequest = req.url.startsWith(environment.apiBaseUrl);
  const isLoginRequest = req.url.includes('/Auth/login');

  const authReq =
    token && isApiRequest
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // A 401 anywhere but the login call itself means the session is no longer valid.
      if (error.status === 401 && !isLoginRequest) {
        auth.logoutAndRedirect();
      }
      return throwError(() => error);
    }),
  );
};
