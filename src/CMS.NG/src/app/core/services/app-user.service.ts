import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { AppUser, AppUserQuery, AppUserRequest } from '@core/models/app-user.model';

@Injectable({ providedIn: 'root' })
export class AppUserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/appusers`;

  getAll(): Observable<AppUser[]> {
    return this.http.get<AppUser[]>(this.baseUrl);
  }

  query(filter: AppUserQuery): Observable<AppUser[]> {
    return this.http.post<AppUser[]>(`${this.baseUrl}/query`, filter);
  }

  getById(userId: string): Observable<AppUser> {
    return this.http.get<AppUser>(`${this.baseUrl}/${encodeURIComponent(userId)}`);
  }

  create(request: AppUserRequest): Observable<AppUser> {
    return this.http.post<AppUser>(this.baseUrl, request);
  }

  update(request: AppUserRequest): Observable<void> {
    return this.http.put<void>(this.baseUrl, request);
  }

  delete(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(userId)}`);
  }

  resetPassword(userId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${encodeURIComponent(userId)}/reset-password`, {});
  }
}
