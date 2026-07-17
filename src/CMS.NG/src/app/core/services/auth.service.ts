import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '@env/environment';
import {
  AuthProfile,
  ChangePasswordRequest,
  LoginRequest,
  ProfileResponse,
  UpdateProfileRequest,
} from '@core/models/auth.model';

/** .NET's ClaimTypes.Role URI — the key role claims land under in the raw JWT payload. */
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/** Session-storage key holding the JSON-serialized {@link AuthProfile}. */
const STORAGE_KEY = 'cms-auth';

/**
 * Holds the signed-in profile (userId, userName, accessToken) in **session** storage and exposes
 * it reactively. Roles are derived from the JWT's claims — no extra API call. Clearing the session
 * (logout or a 401) is the single source of "signed out".
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiBaseUrl}/Auth`;

  private readonly profileSig = signal<AuthProfile | null>(readSession());

  /** The signed-in profile, or null when signed out. */
  readonly profile = this.profileSig.asReadonly();
  readonly userName = computed(() => this.profileSig()?.userName ?? null);
  readonly isAuthenticated = computed(() => !!this.profileSig()?.accessToken);
  /** Roles decoded from the current token's claims. */
  readonly roles = computed(() => rolesFromToken(this.profileSig()?.accessToken));

  /** Bearer token for the interceptor, or null when signed out. */
  get token(): string | null {
    return this.profileSig()?.accessToken ?? null;
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  /** POST credentials; on success persist the profile to session storage. */
  login(request: LoginRequest): Observable<AuthProfile> {
    return this.http
      .post<AuthProfile>(`${this.baseUrl}/login`, request)
      .pipe(tap((profile) => this.setSession(profile)));
  }

  /**
   * Update the signed-in user's display name (PUT /api/Auth/profile). The server renames the JWT
   * user only; on success we refresh the name in the session profile so the app shell updates.
   */
  updateProfile(userName: string): Observable<ProfileResponse> {
    const body: UpdateProfileRequest = { userName };
    return this.http
      .put<ProfileResponse>(`${this.baseUrl}/profile`, body)
      .pipe(tap((res) => this.applyUserName(res.userName)));
  }

  /**
   * Change the signed-in user's password (POST /api/Auth/change-password). Plain-text passwords are
   * sent; the server verifies the current one, enforces complexity, and stores only the new hash.
   * The session/token is unaffected, so nothing local needs updating on success.
   */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, request);
  }

  /** Refresh just the display name in the current session profile (shell + session storage). */
  private applyUserName(userName: string): void {
    const current = this.profileSig();
    if (!current) {
      return;
    }
    this.setSession({ ...current, userName });
  }

  /** Clear the session (does not navigate). */
  logout(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.profileSig.set(null);
  }

  /** Clear the session and return to the login page. */
  logoutAndRedirect(): void {
    this.logout();
    void this.router.navigate(['/login']);
  }

  private setSession(profile: AuthProfile): void {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
    this.profileSig.set(profile);
  }
}

function readSession(): AuthProfile | null {
  const raw = sessionStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return null;
  }
  try {
    const parsed = JSON.parse(raw) as AuthProfile;
    return parsed?.accessToken ? parsed : null;
  } catch {
    return null;
  }
}

/** Decode the JWT payload and pull out role claims (string or array) as a string[]. */
function rolesFromToken(token: string | null | undefined): string[] {
  if (!token) {
    return [];
  }
  const parts = token.split('.');
  if (parts.length < 2) {
    return [];
  }
  try {
    const json = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
    const payload = JSON.parse(json) as Record<string, unknown>;
    const claim = payload[ROLE_CLAIM] ?? payload['role'] ?? payload['roles'];
    if (claim == null) {
      return [];
    }
    return Array.isArray(claim) ? claim.map(String) : [String(claim)];
  } catch {
    return [];
  }
}
