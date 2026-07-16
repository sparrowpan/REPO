import { Component, computed, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';

import { Course } from '@core/models/course.model';
import { AuthService } from '@core/services/auth.service';
import { toProseHtml } from '@core/utils/prose-html.util';
import { CourseQrCode, publicCourseUrl } from '../course-qr-code/course-qr-code';

/** Fields a brochure cannot credibly be sent without. Warned about, never silently omitted. */
const REQUIRED_FIELDS: ReadonlyArray<{ label: string; present: (c: Course) => boolean }> = [
  { label: '課程名稱', present: (c) => !!toProseHtml(c.title) },
  { label: '課程目標', present: (c) => !!toProseHtml(c.objective) },
  { label: '課程大綱', present: (c) => !!toProseHtml(c.outline) },
  { label: '時數', present: (c) => c.hour != null },
];

/**
 * A client-facing course brochure (課程簡介) rendered from the `Course` the detail page already
 * loaded — no fetch, no route, no service call. `course-qr-code` is the precedent for this shape.
 *
 * Two things about it are deliberate and easy to undo by accident:
 *
 * 1. **It renders on screen, not only in print.** An earlier design hid it behind
 *    `@media screen { :host { display: none } }`. That made the print dialog the only preview, and
 *    `display: none` content neither fetches webfonts nor resolves the QR code's async data URL —
 *    so the first Ctrl+P could print a blank QR. Rendering the preview first removes both races.
 * 2. **Absent sections are omitted, never filled with `—`.** See {@link toProseHtml}.
 *
 * The field list below is the whole content contract. Admin columns (pkid, displayOrder, courseId,
 * prodCourseId, friendlyUrl, publishStatus, scheduleOn/Off, courseGroup) must never appear here,
 * and note / otherInfo / listPrice / learningCredit / canRepeat are excluded pending a ruling.
 * **Adding a field is a decision, not a default.**
 */
@Component({
  selector: 'course-brochure-print',
  imports: [DatePipe, CourseQrCode],
  templateUrl: './course-brochure-print.html',
  styleUrl: './course-brochure-print.scss',
})
export class CourseBrochurePrint {
  private readonly auth = inject(AuthService);

  readonly course = input.required<Course>();

  /** Prose sections, normalized for `[innerHTML]`. `null` means "omit this section entirely". */
  protected readonly officialTitle = computed(() => toProseHtml(this.course().officialTitle));
  protected readonly objective = computed(() => toProseHtml(this.course().objective));
  protected readonly target = computed(() => toProseHtml(this.course().target));
  protected readonly prerequisites = computed(() => toProseHtml(this.course().prerequisites));
  protected readonly outline = computed(() => toProseHtml(this.course().outline));
  protected readonly material = computed(() => toProseHtml(this.course().material));
  protected readonly towardCertOrExam = computed(() => toProseHtml(this.course().towardCertOrExam));

  /** The signed-in rep's display name — free from the session, no API call (email/phone are not on the profile). */
  protected readonly repName = this.auth.userName;

  /** A printed brochure goes stale; the footer points the reader at the live page. */
  protected readonly publicUrl = computed(() =>
    publicCourseUrl(this.course().pkid, this.course().courseId),
  );

  protected readonly generatedOn = new Date();

  /** Labels of the required fields this course is missing; empty when the brochure is complete. */
  readonly missingRequired = computed(() =>
    REQUIRED_FIELDS.filter((f) => !f.present(this.course())).map((f) => f.label),
  );
}
