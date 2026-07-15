import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { LookupService } from './lookup.service';
import { TrainingCenterLookup } from '@core/models/training-center-lookup.model';
import { PromotionLookup } from '@core/models/promotion-lookup.model';

describe('LookupService (FeaturedPromoItem lookups)', () => {
  let service: LookupService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/lookups`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LookupService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LookupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getTrainingCenters() GETs /lookups/training-centers', () => {
    const centers: TrainingCenterLookup[] = [
      { pkid: 1, name: '台北' },
      { pkid: 2, name: '新竹' },
    ];
    service.getTrainingCenters().subscribe((rows) => expect(rows).toEqual(centers));
    const req = httpMock.expectOne(`${base}/training-centers`);
    expect(req.request.method).toBe('GET');
    req.flush(centers);
  });

  it('getPromotions() GETs /lookups/promotions with PromoCode for resolution', () => {
    const promotions: PromotionLookup[] = [
      { pkid: 101, promoCode: '20251204_SkillTrainAI', topic: '成為能AI協作的程式設計師', description: '轉職就業養成班' },
    ];
    service.getPromotions().subscribe((rows) => {
      expect(rows.length).toBe(1);
      expect(rows[0].promoCode).toBe('20251204_SkillTrainAI');
    });
    const req = httpMock.expectOne(`${base}/promotions`);
    expect(req.request.method).toBe('GET');
    req.flush(promotions);
  });
});
