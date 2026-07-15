import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { Partner, PartnerQuery, PartnerRequest } from '@core/models/partner.model';
import { PartnerService } from './partner.service';

describe('PartnerService', () => {
  let service: PartnerService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/partners`;

  const sample: Partner = {
    pkid: 1,
    name: '恆逸',
    appKey: 'UUU',
    nameOnPartnerMenu: '恆逸教育訓練中心',
    nameOnCourseDetailPage: '恆逸',
    displayOrder: 1,
    imageFilename: 'uuu.png',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PartnerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /partners', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query() POSTs the filter to /partners/query', () => {
    const filter: PartnerQuery = { keyword: '恆逸' };
    service.query(filter).subscribe((rows) => expect(rows).toEqual([sample]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sample]);
  });

  it('getById() GETs a single partner by pkid', () => {
    service.getById(1).subscribe((row) => expect(row.pkid).toBe(1));
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create() POSTs the request to /partners', () => {
    const request: PartnerRequest = {
      pkid: 0,
      name: '資策會',
      appKey: 'III',
      nameOnPartnerMenu: '資訊工業策進會',
      nameOnCourseDetailPage: '資策會',
      displayOrder: 3,
      imageFilename: null,
    };
    service.create(request).subscribe((row) => expect(row.pkid).toBe(3));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 3, name: '資策會' });
  });

  it('update() PUTs the request to /partners', () => {
    const request: PartnerRequest = { ...sample, pkid: 1, name: '恆逸2' };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the partner by pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
