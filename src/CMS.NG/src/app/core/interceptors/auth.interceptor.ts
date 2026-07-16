import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { environment } from '@env/environment';
import { AuthService } from '@core/services/auth.service';
import { isServerError } from '@core/utils/http-error.util';

/** Shown when the server sends no usable message — a network drop, a proxy error page, a timeout. */
export const FALLBACK_ERROR_MESSAGE = '系統發生錯誤，請稍後再試。An unexpected error occurred.';

/**
 * Pulls the server's safe message out of an error body.
 *
 * Only a non-empty string `message` on an object body is trusted: a 5xx that never reached the API
 * (proxy, dev server) answers with an HTML page, which arrives here as a raw string and must never
 * be rendered into a toast.
 */
function safeMessageOf(error: HttpErrorResponse): string {
  const body: unknown = error.error;
  if (body && typeof body === 'object') {
    const message = (body as { message?: unknown }).message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }
  }
  return FALLBACK_ERROR_MESSAGE;
}

/**
 * Central HTTP policy for the app:
 *
 * - attaches `Authorization: Bearer <token>` to every API request when a token is present;
 * - on a 401 from anything but the login call, clears the session and redirects to login;
 * - on a 5xx from the API, raises one error toast carrying the server's safe message.
 *
 * The error is always re-thrown, so callers still see it. Components are expected to handle only
 * the statuses that mean something to them (400 validation, 409 conflict) and leave 5xx to the
 * toast raised here — otherwise a single failure would surface twice.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const messages = inject(MessageService);
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

      // A 5xx is never actionable by the user and no component can say anything more useful
      // about it, so it is reported once, here. Components skip these via the same predicate.
      if (isApiRequest && isServerError(error)) {
        messages.add({
          severity: 'error',
          summary: '錯誤 Error',
          detail: safeMessageOf(error),
          life: 6000,
        });
      }

      return throwError(() => error);
    }),
  );
};
