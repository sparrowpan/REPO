import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';

import { FeaturedPromoItem, FeaturedPromoItemRequest } from '@core/models/featured-promo-item.model';
import { TrainingCenterLookup } from '@core/models/training-center-lookup.model';
import { PromotionLookup } from '@core/models/promotion-lookup.model';
import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { toIso } from '@core/utils/date.util';
import { isServerError } from '@core/utils/http-error.util';

/** Editable fields of the inline cell form. */
interface EditModel {
  pkid: number;
  promoCode: string;
  promotionPkid: number;
  topic: string;
  description: string;
}

/** Copied cell payload for Copy → Paste. */
interface Clipboard {
  promoCode: string;
  promotionPkid: number;
  topic: string;
  description: string;
}

const SLOTS: readonly number[] = [1, 2, 3];
// getDay(): 0=Sun .. 6=Sat → Chinese weekday character.
const WEEKDAY_CN = ['日', '一', '二', '三', '四', '五', '六'] as const;

@Component({
  selector: 'featured-promo-item-board',
  imports: [CommonModule, FormsModule, InputTextModule, ToastModule, ConfirmDialogModule],
  providers: [ConfirmationService, MessageService],
  templateUrl: './featured-promo-item-board.html',
  styleUrl: './featured-promo-item-board.scss',
})
export class FeaturedPromoItemBoard implements OnInit {
  private readonly service = inject(FeaturedPromoItemService);
  private readonly lookups = inject(LookupService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  protected readonly slots = SLOTS;

  protected readonly centers = signal<TrainingCenterLookup[]>([]);
  protected readonly promotions = signal<PromotionLookup[]>([]);
  protected readonly items = signal<FeaturedPromoItem[]>([]);
  protected readonly loading = signal(false);

  protected readonly activeCenter = signal<number | null>(null);
  protected readonly weekStart = signal<Date>(mondayOf(new Date()));

  // Which cell is being edited (`${dayIso}:${slot}`), plus its working values.
  protected readonly editingKey = signal<string | null>(null);
  protected editModel: EditModel = emptyEdit();
  protected readonly clipboard = signal<Clipboard | null>(null);

  /** The seven dates Monday..Sunday of the active week. */
  protected readonly weekDays = computed<Date[]>(() => {
    const start = this.weekStart();
    return Array.from({ length: 7 }, (_, i) => addDays(start, i));
  });

  /** Header label, e.g. `3/16 -- 3/22`. */
  protected readonly weekLabel = computed(() => {
    const days = this.weekDays();
    return `${md(days[0])} -- ${md(days[6])}`;
  });

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      centers: this.lookups.getTrainingCenters(),
      promotions: this.lookups.getPromotions(),
    }).subscribe({
      next: ({ centers, promotions }) => {
        this.centers.set(centers);
        this.promotions.set(promotions);
        this.activeCenter.set(centers[0]?.pkid ?? null);
        this.loadItems();
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入訓練中心／活動資料。' });
      },
    });
  }

  loadItems(): void {
    const center = this.activeCenter();
    if (center === null) {
      this.items.set([]);
      this.loading.set(false);
      return;
    }
    const days = this.weekDays();
    this.loading.set(true);
    this.service
      .query({
        trainingCenterPkid: center,
        scheduleOnFrom: toIso(days[0]),
        scheduleOnTo: toIso(days[6]),
      })
      .subscribe({
        next: (data) => {
          this.items.set(data);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          if (isServerError(err)) return; // the interceptor already reported this
          this.messages.add({ severity: 'error', summary: '載入失敗', detail: '無法載入上稿資料。' });
        },
      });
  }

  // --- Tabs / week navigation ---------------------------------------------

  selectCenter(pkid: number): void {
    if (this.activeCenter() === pkid) return;
    this.activeCenter.set(pkid);
    this.cancelEdit();
    this.loadItems();
  }

  prevWeek(): void {
    this.weekStart.update((d) => addDays(d, -7));
    this.cancelEdit();
    this.loadItems();
  }

  nextWeek(): void {
    this.weekStart.update((d) => addDays(d, 7));
    this.cancelEdit();
    this.loadItems();
  }

  // --- Cell helpers -------------------------------------------------------

  weekdayLabel(day: Date): string {
    return `${md(day)} (${WEEKDAY_CN[day.getDay()]})`;
  }

  keyOf(day: Date, slot: number): string {
    return `${toIso(day)}:${slot}`;
  }

  itemAt(day: Date, slot: number): FeaturedPromoItem | undefined {
    const iso = toIso(day);
    return this.items().find((i) => i.scheduleOn.slice(0, 10) === iso && i.slot === slot);
  }

  isEditing(day: Date, slot: number): boolean {
    return this.editingKey() === this.keyOf(day, slot);
  }

  // --- Edit / New / Paste -------------------------------------------------

  startEdit(day: Date, slot: number): void {
    const item = this.itemAt(day, slot);
    this.editModel = item
      ? { pkid: item.pkid, promoCode: item.promoCode, promotionPkid: item.promotionPkid, topic: item.topic, description: item.description }
      : emptyEdit();
    this.editingKey.set(this.keyOf(day, slot));
  }

  startPaste(day: Date, slot: number): void {
    const clip = this.clipboard();
    if (!clip) return;
    this.editModel = { pkid: 0, ...clip };
    this.editingKey.set(this.keyOf(day, slot));
  }

  cancelEdit(): void {
    this.editingKey.set(null);
    this.editModel = emptyEdit();
  }

  /**
   * Resolve the typed PromoCode against the Promotion2 lookup: set promotionPkid, and default
   * Topic / Description from the matched promotion when they are still blank. Returns the match.
   */
  resolvePromoCode(): PromotionLookup | undefined {
    const code = this.editModel.promoCode.trim();
    const match = this.promotions().find((p) => p.promoCode.toLowerCase() === code.toLowerCase());
    if (match) {
      this.editModel.promotionPkid = match.pkid;
      if (!this.editModel.topic.trim()) this.editModel.topic = match.topic;
      if (!this.editModel.description.trim()) this.editModel.description = match.description;
    } else {
      this.editModel.promotionPkid = 0;
    }
    return match;
  }

  save(day: Date, slot: number): void {
    this.resolvePromoCode();
    if (!this.editModel.promotionPkid) {
      this.messages.add({ severity: 'warn', summary: '找不到活動', detail: `查無活動代碼「${this.editModel.promoCode}」。` });
      return;
    }
    if (!this.editModel.topic.trim() || !this.editModel.description.trim()) {
      this.messages.add({ severity: 'warn', summary: '欄位不完整', detail: '請填寫標題與描述。' });
      return;
    }

    const request: FeaturedPromoItemRequest = {
      pkid: this.editModel.pkid,
      scheduleOn: toIso(day)!,
      trainingCenterPkid: this.activeCenter()!,
      slot,
      promotionPkid: this.editModel.promotionPkid,
      topic: this.editModel.topic.trim(),
      description: this.editModel.description.trim(),
    };

    const done = () => {
      this.messages.add({ severity: 'success', summary: '已儲存', detail: '上稿資料已儲存。' });
      this.cancelEdit();
      this.loadItems();
    };
    const fail = (err: HttpErrorResponse) => {
      if (isServerError(err)) return; // the interceptor already reported this
      this.messages.add({ severity: 'error', summary: '儲存失敗', detail: '儲存上稿資料時發生錯誤。' });
    };

    if (request.pkid > 0) {
      this.service.update(request).subscribe({ next: done, error: fail });
    } else {
      this.service.create(request).subscribe({ next: done, error: fail });
    }
  }

  // --- Copy / Delete / Move ----------------------------------------------

  copy(item: FeaturedPromoItem): void {
    this.clipboard.set({
      promoCode: item.promoCode,
      promotionPkid: item.promotionPkid,
      topic: item.topic,
      description: item.description,
    });
    this.messages.add({ severity: 'info', summary: '已複製', detail: `已複製「${item.promoCode}」。` });
  }

  confirmDelete(item: FeaturedPromoItem): void {
    this.confirmation.confirm({
      header: '刪除確認',
      message: `確定要刪除版位 <b>${item.slot}</b>「${item.promoCode}」？`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: '刪除',
      rejectLabel: '取消',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => this.delete(item),
    });
  }

  private delete(item: FeaturedPromoItem): void {
    this.service.delete(item.pkid).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: '已刪除', detail: '上稿資料已刪除。' });
        this.loadItems();
      },
      error: (err: HttpErrorResponse) => {
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '刪除失敗', detail: '刪除上稿資料時發生錯誤。' });
      },
    });
  }

  /** `+` — push the item down one slot (1 → 2 → 3), swapping with any occupant. */
  moveDown(item: FeaturedPromoItem): void {
    if (item.slot >= 3) return;
    this.move(item, item.slot + 1);
  }

  /** `−` — pull the item up one slot (3 → 2 → 1), swapping with any occupant. */
  moveUp(item: FeaturedPromoItem): void {
    if (item.slot <= 1) return;
    this.move(item, item.slot - 1);
  }

  private move(item: FeaturedPromoItem, targetSlot: number): void {
    this.cancelEdit();
    this.service.move({ pkid: item.pkid, targetSlot }).subscribe({
      next: () => this.loadItems(),
      error: (err: HttpErrorResponse) => {
        if (isServerError(err)) return; // the interceptor already reported this
        this.messages.add({ severity: 'error', summary: '搬移失敗', detail: '搬移版位時發生錯誤。' });
      },
    });
  }
}

// --- Local date/edit helpers ----------------------------------------------

function emptyEdit(): EditModel {
  return { pkid: 0, promoCode: '', promotionPkid: 0, topic: '', description: '' };
}

/** Monday of the week containing `date` (local, time zeroed). */
function mondayOf(date: Date): Date {
  const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const day = d.getDay(); // 0=Sun..6=Sat
  d.setDate(d.getDate() + (day === 0 ? -6 : 1 - day));
  return d;
}

function addDays(date: Date, days: number): Date {
  const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  d.setDate(d.getDate() + days);
  return d;
}

/** `M/D` (no leading zero) — matches the board mockups. */
function md(date: Date): string {
  return `${date.getMonth() + 1}/${date.getDate()}`;
}
