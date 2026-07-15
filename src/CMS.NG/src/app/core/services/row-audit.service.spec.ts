import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '@env/environment';
import { RowAuditEntry } from '@core/models/row-audit.model';
import { RowAuditService } from './row-audit.service';

describe('RowAuditService', () => {
  let service: RowAuditService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/rowaudit`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RowAuditService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(RowAuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getForRecord() GETs /rowaudit with tableName + pkid query params', () => {
    const entries: RowAuditEntry[] = [
      { dateTime: '2026-06-04T14:30:00', userName: 'alice', actionType: 'Update', actionDesc: 'Title' },
    ];

    service.getForRecord('Course', 123).subscribe((res) => expect(res).toEqual(entries));

    const req = httpMock.expectOne((r) => r.url === base);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('tableName')).toBe('Course');
    expect(req.request.params.get('pkid')).toBe('123');
    req.flush(entries);
  });
});
