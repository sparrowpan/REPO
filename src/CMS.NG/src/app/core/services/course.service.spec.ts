import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { Course, CourseQuery, CourseRequest } from '@core/models/course.model';
import { CourseService } from './course.service';

describe('CourseService', () => {
  let service: CourseService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/courses`;

  const sample = { pkid: 1, title: 'AZ-900' } as Course;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CourseService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /courses', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query() POSTs the filter to /courses/query', () => {
    const filter: CourseQuery = { keyword: 'AZ', partnerPkid: 1 };
    service.query(filter).subscribe((rows) => expect(rows).toEqual([sample]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sample]);
  });

  it('getById() GETs a single course by pkid', () => {
    service.getById(1).subscribe((row) => expect(row.pkid).toBe(1));
    const req = httpMock.expectOne(`${base}/1`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create() POSTs the request to /courses', () => {
    const request = { pkid: 0, title: 'New' } as CourseRequest;
    service.create(request).subscribe((row) => expect(row.pkid).toBe(9));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 9 });
  });

  it('update() PUTs the request to /courses', () => {
    const request = { pkid: 1, title: 'Edit' } as CourseRequest;
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the course by pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
