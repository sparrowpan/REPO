/** Response model for a course group (課程群組). */
export interface CourseGroup {
  pkid: number;
  description: string;
}

/** Write DTO for creating / updating a course group. */
export interface CourseGroupRequest {
  pkid: number;
  description: string;
}

/** Search DTO for filtering course groups. */
export interface CourseGroupQuery {
  keyword?: string | null;
}
