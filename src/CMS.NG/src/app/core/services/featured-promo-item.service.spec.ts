import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import {
  FeaturedPromoItem,
  FeaturedPromoItemQuery,
  FeaturedPromoItemRequest,
} from '@core/models/featured-promo-item.model';
import { FeaturedPromoItemService } from './featured-promo-item.service';

describe('FeaturedPromoItemService', () => {
  let service: FeaturedPromoItemService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/featured-promo-items`;

  const sample: FeaturedPromoItem = {
    pkid: 1,
    scheduleOn: '2026-03-16',
    trainingCenterPkid: 1,
    slot: 1,
    promotionPkid: 101,
    promoCode: '20251204_SkillTrainAI',
    topic: '成為能AI協作的程式設計師',
    description: '轉職就業養成班',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [FeaturedPromoItemService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeaturedPromoItemService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /featured-promo-items', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query() POSTs the week/center filter to /query', () => {
    const filter: FeaturedPromoItemQuery = {
      trainingCenterPkid: 1,
      scheduleOnFrom: '2026-03-16',
      scheduleOnTo: '2026-03-22',
    };
    service.query(filter).subscribe((rows) => expect(rows).toEqual([sample]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sample]);
  });

  it('getById() GETs a single item by pkid', () => {
    service.getById(1).subscribe((row) => expect(row.pkid).toBe(1));
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create() POSTs the request to /featured-promo-items', () => {
    const request: FeaturedPromoItemRequest = {
      pkid: 0,
      scheduleOn: '2026-03-18',
      trainingCenterPkid: 1,
      slot: 2,
      promotionPkid: 102,
      topic: 'Google AI工具一次掌握',
      description: '不需技術基礎',
    };
    service.create(request).subscribe((row) => expect(row.pkid).toBe(9));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 9 });
  });

  it('update() PUTs the request to /featured-promo-items', () => {
    const request: FeaturedPromoItemRequest = {
      pkid: 1,
      scheduleOn: '2026-03-16',
      trainingCenterPkid: 1,
      slot: 1,
      promotionPkid: 103,
      topic: 'n8n自動化三部曲',
      description: '從自動化新手到企業級架構師',
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the item by pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('move() POSTs pkid + targetSlot to /move', () => {
    service.move({ pkid: 1, targetSlot: 2 }).subscribe();
    const req = httpMock.expectOne(`${base}/move`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ pkid: 1, targetSlot: 2 });
    req.flush(null);
  });
});
