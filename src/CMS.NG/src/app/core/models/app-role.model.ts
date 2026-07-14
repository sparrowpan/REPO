/** Response model for an application role (角色). */
export interface AppRole {
  pkid: number;
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description: string | null;
  /** 使用者數 — number of users assigned to this role. */
  userCount: number;
  /** Assigned user ids (populated on GET by id). */
  userIds: string[];
}

/** Write DTO for creating / updating a role. */
export interface AppRoleRequest {
  pkid: number;
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description: string | null;
  userIds: string[];
}

/** Search DTO for filtering roles. */
export interface AppRoleQuery {
  keyword?: string | null;
  permissionLevel?: number | null;
}
