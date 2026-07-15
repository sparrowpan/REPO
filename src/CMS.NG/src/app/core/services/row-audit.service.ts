import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { RowAuditEntry } from '@core/models/row-audit.model';

/** Reads a single record's audit history (異動紀錄) from `GET /api/rowaudit`. */
@Injectable({ providedIn: 'root' })
export class RowAuditService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/rowaudit`;

  /** The audit trail for one record (by table name + surrogate pkid), newest first. */
  getForRecord(tableName: string, pkid: number): Observable<RowAuditEntry[]> {
    const params = new HttpParams().set('tableName', tableName).set('pkid', pkid);
    return this.http.get<RowAuditEntry[]>(this.baseUrl, { params });
  }
}
