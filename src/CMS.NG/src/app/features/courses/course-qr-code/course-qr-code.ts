import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import QRCode from 'qrcode';

/**
 * The public course page on the main site — a different system to this CMS, and the only
 * course URL a client can actually open. Shared with the brochure, which prints it as the
 * 「最新版本」 footer alongside the QR code encoding the same address.
 */
export function publicCourseUrl(pkid: number, courseId: string): string {
  return `https://www.uuu.com.tw/Course/Show/${pkid}/${courseId}`;
}

/**
 * Renders a downloadable QR code for a course. The encoded target is the public
 * course page — `https://www.uuu.com.tw/Course/Show/{pkid}/{courseId}` — and the
 * CourseId is shown as the title above it.
 */
@Component({
  selector: 'course-qr-code',
  imports: [ButtonModule],
  templateUrl: './course-qr-code.html',
  styleUrl: './course-qr-code.scss',
})
export class CourseQrCode {
  private readonly document = inject(DOCUMENT);

  readonly pkid = input.required<number>();
  readonly courseId = input.required<string>();

  /** Public course page the QR code points at. */
  readonly url = computed(() => publicCourseUrl(this.pkid(), this.courseId()));

  /** PNG data URL of the rendered QR code (empty until the first render resolves). */
  protected readonly dataUrl = signal('');

  constructor() {
    effect(() => {
      void this.render(this.url());
    });
  }

  /** Encode `target` as a PNG data URL and store it for display / download. */
  async render(target = this.url()): Promise<void> {
    this.dataUrl.set(
      await QRCode.toDataURL(target, { width: 200, margin: 1, errorCorrectionLevel: 'M' }),
    );
  }

  /** Download the generated QR code as a PNG image named after the CourseId. */
  download(): void {
    const image = this.dataUrl();
    if (!image) return;
    const link = this.document.createElement('a');
    link.href = image;
    link.download = `${this.courseId()}-qrcode.png`;
    link.click();
  }
}
