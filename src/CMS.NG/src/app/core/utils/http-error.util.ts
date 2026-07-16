import { HttpErrorResponse } from '@angular/common/http';

/**
 * True when a failed request is the server's fault (5xx) rather than the caller's.
 *
 * `authInterceptor` raises the single error toast for these, so a component's own `error` handler
 * should run its state cleanup and then bail out instead of reporting the failure a second time:
 *
 * ```ts
 * error: (err: HttpErrorResponse) => {
 *   this.loading.set(false);
 *   if (isServerError(err)) return; // already reported by the interceptor
 *   this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入資料。' });
 * }
 * ```
 */
export function isServerError(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status >= 500;
}
