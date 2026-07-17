import { PartnerLookup } from '@core/models/partner-lookup.model';
import { CourseGroupLookup } from '@core/models/course-group-lookup.model';
import { PublishStatusLookup } from '@core/models/publish-status-lookup.model';
import { JobCategoryLookup } from '@core/models/job-category-lookup.model';
import { CertificationLookup } from '@core/models/certification-lookup.model';

/** Response model for a course (課程). */
export interface Course {
  pkid: number;
  title: string;
  officialTitle: string | null;
  courseId: string;
  prodCourseId: string;
  friendlyUrl: string;
  displayOrder: number;
  partnerPkid: number;
  courseGroupPkid: number | null;
  publishStatusPkid: number;
  scheduleOn: string; // ISO date
  scheduleOff: string; // ISO date
  hour: number;
  listPrice: number;
  learningCredit: number;
  material: string | null;
  objective: string | null;
  target: string | null;
  prerequisites: string | null;
  outline: string | null;
  towardCertOrExam: string | null;
  note: string | null;
  otherInfo: string | null;
  canRepeat: boolean;
  partner: PartnerLookup | null;
  courseGroup: CourseGroupLookup | null;
  publishStatus: PublishStatusLookup | null;
  jobCategoryCount: number;
  certificationCount: number;
  jobCategoryPkids: number[];
  certificationPkids: number[];
  jobCategories: JobCategoryLookup[];
  certifications: CertificationLookup[];
}

/** Write DTO for creating / updating a course. */
export interface CourseRequest {
  pkid: number;
  title: string;
  officialTitle: string | null;
  courseId: string;
  prodCourseId: string;
  friendlyUrl: string;
  displayOrder: number;
  partnerPkid: number;
  courseGroupPkid: number | null;
  publishStatusPkid: number;
  scheduleOn: string; // ISO date (yyyy-MM-dd)
  scheduleOff: string; // ISO date (yyyy-MM-dd)
  hour: number;
  listPrice: number;
  learningCredit: number;
  material: string | null;
  objective: string | null;
  target: string | null;
  prerequisites: string | null;
  outline: string | null;
  towardCertOrExam: string | null;
  note: string | null;
  otherInfo: string | null;
  canRepeat: boolean;
  jobCategoryPkids: number[];
  certificationPkids: number[];
}

/** Search DTO for filtering courses. */
export interface CourseQuery {
  keyword?: string | null;
  partnerPkid?: number | null;
  courseGroupPkid?: number | null;
  publishStatusPkid?: number | null;
  canRepeat?: boolean | null;
  scheduleOnFrom?: string | null;
  scheduleOnTo?: string | null;
  scheduleOffFrom?: string | null;
  scheduleOffTo?: string | null;
}
