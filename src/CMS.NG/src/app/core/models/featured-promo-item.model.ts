/**
 * Response model for a featured promo item (上稿作業) — one promo occupying a
 * (scheduleOn, trainingCenter, slot) cell on the weekly home-page board.
 */
export interface FeaturedPromoItem {
  pkid: number;
  scheduleOn: string; // ISO yyyy-MM-dd
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  promoCode: string; // joined from Promotion2
  topic: string;
  description: string;
}

/** Write DTO for creating / updating a featured promo item. */
export interface FeaturedPromoItemRequest {
  pkid: number;
  scheduleOn: string;
  trainingCenterPkid: number;
  slot: number;
  promotionPkid: number;
  topic: string;
  description: string;
}

/** Search DTO — one TrainingCenter tab + one Monday–Sunday week. */
export interface FeaturedPromoItemQuery {
  trainingCenterPkid?: number | null;
  scheduleOnFrom?: string | null;
  scheduleOnTo?: string | null;
  slot?: number | null;
}

/** Body for the slot move endpoint (+ / − on the board). */
export interface MoveSlotRequest {
  pkid: number;
  targetSlot: number;
}
