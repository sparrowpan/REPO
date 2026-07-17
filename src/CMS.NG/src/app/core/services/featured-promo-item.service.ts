import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import {
  FeaturedPromoItem,
  FeaturedPromoItemQuery,
  FeaturedPromoItemRequest,
  MoveSlotRequest,
} from '@core/models/featured-promo-item.model';

@Injectable({ providedIn: 'root' })
export class FeaturedPromoItemService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/featured-promo-items`;

  getAll(): Observable<FeaturedPromoItem[]> {
    return this.http.get<FeaturedPromoItem[]>(this.baseUrl);
  }

  query(filter: FeaturedPromoItemQuery): Observable<FeaturedPromoItem[]> {
    return this.http.post<FeaturedPromoItem[]>(`${this.baseUrl}/query`, filter);
  }

  getById(pkid: number): Observable<FeaturedPromoItem> {
    return this.http.get<FeaturedPromoItem>(`${this.baseUrl}/${pkid}`);
  }

  create(request: FeaturedPromoItemRequest): Observable<FeaturedPromoItem> {
    return this.http.post<FeaturedPromoItem>(this.baseUrl, request);
  }

  update(request: FeaturedPromoItemRequest): Observable<void> {
    return this.http.put<void>(this.baseUrl, request);
  }

  delete(pkid: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${pkid}`);
  }

  /** Move an item to a different slot on the same day (swaps with any occupant). */
  move(request: MoveSlotRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/move`, request);
  }
}
