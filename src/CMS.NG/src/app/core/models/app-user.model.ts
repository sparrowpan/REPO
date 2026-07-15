/** Response model for an application login account (使用者). PasswordHash is never exposed. */
export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  /** 密碼更新時間 — read-only; set by the backend on create / password reset. */
  passwordUpdatedTime: string | null;
  /** 角色數 — number of roles assigned to this user. */
  roleCount: number;
  /** Assigned role ids (populated on GET by id). */
  roleIds: string[];
}

/** Write DTO for creating / updating a user. No password field — handled backend-side. */
export interface AppUserRequest {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
}

/** Search DTO for filtering users. */
export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
}
