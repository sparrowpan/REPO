/** One row of a record's audit history (異動紀錄), as returned by `GET /api/rowaudit`. */
export interface RowAuditEntry {
  /** When the change happened (ISO 8601 string from the server). */
  dateTime: string;
  /** The user who made the change. */
  userName: string;
  /** "Insert" | "Update" | "Delete". */
  actionType: string;
  /** Insert/Delete: the row's first string column; Update: the changed column names. */
  actionDesc: string | null;
}
