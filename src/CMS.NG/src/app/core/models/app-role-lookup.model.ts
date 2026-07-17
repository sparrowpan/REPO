/** Slim AppRole lookup item for the user's n-n role select. */
export interface AppRoleLookup {
  roleId: string;
  roleName: string;
  /** Display label — "RoleName (RoleId)". */
  label: string;
}
