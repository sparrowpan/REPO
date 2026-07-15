/** Slim Promotion2 lookup — resolve a typed PromoCode to its pkid + default Topic / Description. */
export interface PromotionLookup {
  pkid: number;
  promoCode: string;
  topic: string;
  description: string;
}
