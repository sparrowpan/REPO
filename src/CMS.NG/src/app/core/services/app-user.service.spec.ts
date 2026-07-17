import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { AppUser, AppUserQuery, AppUserRequest } from '@core/models/app-user.model';
import { AppUserService } from './app-user.service';

describe('AppUserService', () => {
  let service: AppUserService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/appusers`;

  const sampleUser: AppUser = {
    pkid: 1,
    userId: 'helen',
    userName: 'Helen Wang',
    isActive: true,
    passwordUpdatedTime: '2026-01-01T00:00:00',
    roleCount: 2,
    roleIds: ['Admin', 'User'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AppUserService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppUserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /appusers', () => {
    service.getAll().subscribe((users) => expect(users.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleUser]);
  });

  it('query() POSTs the filter to /appusers/query', () => {
    const filter: AppUserQuery = { keyword: 'helen', isActive: true };
    service.query(filter).subscribe((users) => expect(users).toEqual([sampleUser]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sampleUser]);
  });

  it('getById() GETs a single encoded user', () => {
    service.getById('a/b').subscribe((user) => expect(user.userId).toBe('helen'));
    const req = httpMock.expectOne(`${base}/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleUser);
  });

  it('create() POSTs the request to /appusers', () => {
    const request: AppUserRequest = {
      pkid: 0,
      userId: 'jenny',
      userName: 'Jenny',
      isActive: true,
      roleIds: ['User'],
    };
    service.create(request).subscribe((user) => expect(user.userId).toBe('jenny'));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleUser, userId: 'jenny' });
  });

  it('update() PUTs the request to /appusers', () => {
    const request: AppUserRequest = {
      pkid: 1,
      userId: 'helen',
      userName: 'Helen 2',
      isActive: false,
      roleIds: [],
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the encoded user id', () => {
    service.delete('a/b').subscribe();
    const req = httpMock.expectOne(`${base}/a%2Fb`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('resetPassword() POSTs to the encoded reset-password route', () => {
    service.resetPassword('a/b').subscribe();
    const req = httpMock.expectOne(`${base}/a%2Fb/reset-password`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });
});
