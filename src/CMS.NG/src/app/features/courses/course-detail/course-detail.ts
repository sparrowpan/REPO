import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { Course } from '@core/models/course.model';
import { CourseService } from '@core/services/course.service';
import { CourseQrCode } from '../course-qr-code/course-qr-code';

@Component({
  selector: 'course-detail',
  imports: [CommonModule, RouterLink, ButtonModule, TagModule, ToastModule, CourseQrCode],
  providers: [MessageService],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseService);
  private readonly messages = inject(MessageService);

  protected readonly course = signal<Course | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const pkid = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(pkid).subscribe({
      next: (course) => {
        this.course.set(course);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入課程資料。' });
        this.loading.set(false);
      },
    });
  }

  edit(): void {
    this.router.navigate(['/courses', this.course()!.pkid, 'edit']);
  }

  back(): void {
    this.router.navigate(['/courses']);
  }
}
