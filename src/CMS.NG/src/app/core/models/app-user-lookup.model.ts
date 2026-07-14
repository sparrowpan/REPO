/** Slim AppUser lookup item for the role's n-n user select. */
export interface AppUserLookup {
  userId: string;
  userName: string;
  /** Display label — "UserName (UserId)". */
  label: string;
}
