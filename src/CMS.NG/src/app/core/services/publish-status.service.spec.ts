import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import {
  PublishStatus,
  PublishStatusQuery,
  PublishStatusRequest,
} from '@core/models/publish-status.model';
import { PublishStatusService } from './publish-status.service';

describe('PublishStatusService', () => {
  let service: PublishStatusService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/publish-statuses`;

  const sample: PublishStatus = {
    pkid: 1,
    description: '草稿',
    isDraft: true,
    isPublished: false,
    isDiscontinued: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PublishStatusService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PublishStatusService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /publish-statuses', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query() POSTs the filter to /publish-statuses/query', () => {
    const filter: PublishStatusQuery = { keyword: '草稿', isDraft: true };
    service.query(filter).subscribe((rows) => expect(rows).toEqual([sample]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sample]);
  });

  it('getById() GETs a single status by pkid', () => {
    service.getById(1).subscribe((row) => expect(row.pkid).toBe(1));
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create() POSTs the request to /publish-statuses', () => {
    const request: PublishStatusRequest = {
      pkid: 4,
      description: '已封存',
      isDraft: false,
      isPublished: false,
      isDiscontinued: true,
    };
    service.create(request).subscribe((row) => expect(row.pkid).toBe(4));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 4, description: '已封存' });
  });

  it('update() PUTs the request to /publish-statuses', () => {
    const request: PublishStatusRequest = {
      pkid: 1,
      description: '草稿2',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the status by pkid', () => {
    service.delete(3).subscribe();
    const req = httpMock.expectOne(`${base}/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
