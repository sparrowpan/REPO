import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Title } from '@angular/platform-browser';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { Course } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';
import { isServerError } from '@core/utils/http-error.util';
import { CourseQrCode } from '../course-qr-code/course-qr-code';
import { CourseBrochurePrint } from '../course-brochure-print/course-brochure-print';

@Component({
  selector: 'course-detail',
  imports: [
    CommonModule,
    RouterLink,
    ButtonModule,
    TagModule,
    ToastModule,
    CourseQrCode,
    CourseBrochurePrint,
    RowAuditBadge,
  ],
  providers: [MessageService],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly messages = inject(MessageService);
  private readonly document = inject(DOCUMENT);
  private readonly title = inject(Title);

  /** Restored on destroy — without this the tab reads 課程簡介_… on every later route. */
  private readonly originalTitle = this.title.getTitle();

  protected readonly course = signal<Course | null>(null);
  protected readonly loading = signal(true);

  /**
   * Whether the 課程簡介 preview is showing. Printing is deliberately gated on it: the brochure is
   * only in the DOM while the preview is open, so Ctrl+P with it closed behaves exactly as it did
   * before this feature, and printing it never races the QR code's async data URL.
   */
  protected readonly showBrochure = signal(false);

  ngOnInit(): void {
    const pkid = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(pkid).subscribe({
      next: (course) => {
        this.course.set(course);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入課程資料。' });
      },
    });
  }

  ngOnDestroy(): void {
    this.title.setTitle(this.originalTitle);
  }

  toggleBrochure(): void {
    this.showBrochure.update((shown) => !shown);
  }

  /** Print the brochure. Chrome's Save-as-PDF filename defaults to `document.title`. */
  printBrochure(): void {
    const course = this.course();
    if (!course) return;
    this.title.setTitle(`課程簡介_${course.title}`);
    this.document.defaultView?.print();
  }

  edit(): void {
    this.router.navigate(['/courses', this.course()!.pkid, 'edit']);
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
