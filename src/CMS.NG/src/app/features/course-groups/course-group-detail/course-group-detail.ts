import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';

import { CourseGroup } from '@core/models/course-group.model';
import { CourseGroupService } from '@core/services/course-group.service';
import { RowAuditBadge } from '@core/components/row-audit-badge/row-audit-badge';

@Component({
  selector: 'course-group-detail',
  imports: [CommonModule, ButtonModule, ToastModule, RowAuditBadge],
  providers: [MessageService],
  templateUrl: './course-group-detail.html',
  styleUrl: './course-group-detail.scss',
})
export class CourseGroupDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CourseGroupService);
  private readonly messages = inject(MessageService);

  protected readonly courseGroup = signal<CourseGroup | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const pkid = Number(this.route.snapshot.paramMap.get('id'));
    this.service.getById(pkid).subscribe({
      next: (courseGroup) => {
        this.courseGroup.set(courseGroup);
        this.loading.set(false);
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入課程群組資料。' });
        this.loading.set(false);
      },
    });
  }

  edit(): void {
    this.router.navigate(['/course-groups', this.courseGroup()!.pkid, 'edit']);
  }

  back(): void {
    this.router.navigate(['/course-groups']);
  }
}
