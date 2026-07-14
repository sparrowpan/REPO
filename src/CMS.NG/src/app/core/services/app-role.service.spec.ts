import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { AppRole, AppRoleQuery, AppRoleRequest } from '@core/models/app-role.model';
import { AppRoleService } from './app-role.service';

describe('AppRoleService', () => {
  let service: AppRoleService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/approles`;

  const sampleRole: AppRole = {
    pkid: 1,
    roleId: 'Admin',
    roleName: 'Administrator',
    permissionLevel: 1,
    description: '系統管理員',
    userCount: 3,
    userIds: ['helen'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AppRoleService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppRoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAll() issues a GET to /approles', () => {
    service.getAll().subscribe((roles) => expect(roles.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sampleRole]);
  });

  it('query() POSTs the filter to /approles/query', () => {
    const filter: AppRoleQuery = { keyword: 'admin', permissionLevel: 1 };
    service.query(filter).subscribe((roles) => expect(roles).toEqual([sampleRole]));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(filter);
    req.flush([sampleRole]);
  });

  it('getById() GETs a single encoded role', () => {
    service.getById('Admin/1').subscribe((role) => expect(role.roleId).toBe('Admin'));
    const req = httpMock.expectOne(`${base}/Admin%2F1`);
    expect(req.request.method).toBe('GET');
    req.flush(sampleRole);
  });

  it('create() POSTs the request to /approles', () => {
    const request: AppRoleRequest = {
      pkid: 0,
      roleId: 'Editor',
      roleName: 'Editor',
      permissionLevel: 50,
      description: null,
      userIds: ['helen'],
    };
    service.create(request).subscribe((role) => expect(role.roleId).toBe('Editor'));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sampleRole, roleId: 'Editor' });
  });

  it('update() PUTs the request to /approles', () => {
    const request: AppRoleRequest = {
      pkid: 1,
      roleId: 'Admin',
      roleName: 'Admin 2',
      permissionLevel: 1,
      description: null,
      userIds: [],
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null);
  });

  it('delete() DELETEs the encoded role id', () => {
    service.delete('Admin').subscribe();
    const req = httpMock.expectOne(`${base}/Admin`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
