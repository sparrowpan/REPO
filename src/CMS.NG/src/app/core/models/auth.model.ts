/** Credentials posted to POST /api/Auth/login. */
export interface LoginRequest {
  userId: string;
  password: string;
}

/** Profile returned by the login endpoint and persisted in session storage. */
export interface AuthProfile {
  userId: string;
  userName: string;
  accessToken: string;
}

/** Body of PUT /api/Auth/profile — only the editable display name (UserId comes from the JWT). */
export interface UpdateProfileRequest {
  userName: string;
}

/** Response from PUT /api/Auth/profile — the saved (trimmed) name for the signed-in user. */
export interface ProfileResponse {
  userId: string;
  userName: string;
}

/**
 * Body of POST /api/Auth/change-password. Plain-text passwords only — never a hash. The target user
 * is taken from the JWT server-side, so no UserId is (or should be) sent.
 */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}
