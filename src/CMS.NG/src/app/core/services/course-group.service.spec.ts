import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { CourseGroup, CourseGroupQuery, CourseGroupRequest } from '@core/models/course-group.model';
import { CourseGroupService } from './course-group.service';

describe('CourseGroupService', () => {
  let service: CourseGroupService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/course-groups`;

  const sample: CourseGroup = {
    pkid: 1,
    description: '資訊技術',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CourseGroupService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseGroupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /course-groups', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query() POSTs the filter to /course-groups/query', () => {
    const filter: CourseGroupQuery = { keyword: '資訊' };
    service.query(filter).subscribe((rows) => expect(rows).toEqual([sample]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sample]);
  });

  it('getById() GETs a single course group by pkid', () => {
    service.getById(1).subscribe((row) => expect(row.pkid).toBe(1));
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create() POSTs the request to /course-groups', () => {
    const request: CourseGroupRequest = { pkid: 0, description: '設計創意' };
    service.create(request).subscribe((row) => expect(row.pkid).toBe(3));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 3, description: '設計創意' });
  });

  it('update() PUTs the request to /course-groups', () => {
    const request: CourseGroupRequest = { ...sample, pkid: 1, description: '資訊技術2' };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the course group by pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
