import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';

import { RowAuditEntry } from '@core/models/row-audit.model';
import { RowAuditService } from '@core/services/row-audit.service';

/**
 * Reusable toolbar badge that shows a record's Row Audit history (異動紀錄 History).
 *
 * Drop it in any detail/form page's toolbar with the page's `tableName` and the current record's `pkid`.
 * On load it fetches the record's trail and shows the most recent change inline on the badge (so the user
 * sees the latest edit without clicking); clicking opens a dialog listing the full trail, newest first.
 * With no history yet — or before a new record is saved (falsy pkid) — it shows a neutral "no history" state.
 */
@Component({
  selector: 'row-audit-badge',
  imports: [CommonModule, ButtonModule, DialogModule],
  templateUrl: './row-audit-badge.html',
  styleUrl: './row-audit-badge.scss',
})
export class RowAuditBadge {
  private readonly service = inject(RowAuditService);

  /** The business table this record belongs to (e.g. "Course"). */
  readonly tableName = input.required<string>();
  /** The record's surrogate pkid. Null/0 (e.g. an unsaved new record) → nothing to fetch. */
  readonly pkid = input<number | null>(null);

  protected readonly entries = signal<RowAuditEntry[]>([]);
  protected readonly loading = signal(false);
  protected readonly dialogVisible = signal(false);

  /** The most recent audit entry (the endpoint returns newest first), or null when there is no history. */
  protected readonly latest = computed(() => this.entries()[0] ?? null);

  constructor() {
    // Refetch whenever the target record changes (e.g. a form loads its record after init).
    effect(() => {
      const tableName = this.tableName();
      const pkid = this.pkid();
      if (!tableName || !pkid) {
        this.entries.set([]);
        return;
      }
      this.fetch(tableName, pkid);
    });
  }

  private fetch(tableName: string, pkid: number): void {
    this.loading.set(true);
    this.service.getForRecord(tableName, pkid).subscribe({
      next: (entries) => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => {
        this.entries.set([]);
        this.loading.set(false);
      },
    });
  }

  open(): void {
    this.dialogVisible.set(true);
  }
}
