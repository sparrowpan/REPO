import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { RowAuditBadge } from './row-audit-badge';
import { RowAuditService } from '@core/services/row-audit.service';
import { RowAuditEntry } from '@core/models/row-audit.model';

// Tracked so afterEach can destroy it — the dialog is appended to document.body, so an
// un-destroyed fixture would leak its markup into the next test's body queries.
let fixture: ComponentFixture<RowAuditBadge>;

const trail: RowAuditEntry[] = [
  { dateTime: '2026-06-04T14:30:00', userName: 'alice', actionType: 'Update', actionDesc: 'Title, Description' },
  { dateTime: '2026-06-02T11:15:00', userName: 'carol', actionType: 'Update', actionDesc: 'IsPublished' },
  { dateTime: '2026-06-01T09:00:00', userName: 'bob', actionType: 'Insert', actionDesc: 'C# 101' },
];

function setup(entries: RowAuditEntry[], pkid: number | null = 123) {
  const serviceSpy = jasmine.createSpyObj<RowAuditService>('RowAuditService', ['getForRecord']);
  serviceSpy.getForRecord.and.returnValue(of(entries));

  TestBed.configureTestingModule({
    imports: [RowAuditBadge],
    providers: [provideNoopAnimations(), { provide: RowAuditService, useValue: serviceSpy }],
  });

  fixture = TestBed.createComponent(RowAuditBadge);
  fixture.componentRef.setInput('tableName', 'Course');
  fixture.componentRef.setInput('pkid', pkid);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy };
}

describe('RowAuditBadge', () => {
  afterEach(() => fixture.destroy());

  it('fetches the record history on load and shows the most recent entry inline', () => {
    const { fixture, serviceSpy } = setup(trail);

    expect(serviceSpy.getForRecord).toHaveBeenCalledWith('Course', 123);

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('異動紀錄 History');
    // The newest entry (index 0) is surfaced without opening the dialog.
    expect(text).toContain('Update by alice');
    expect(text).toContain('2026-06-04 14:30');
  });

  it('opens the dialog listing the full audit trail, newest first', () => {
    const { fixture, component } = setup(trail);

    component.open();
    fixture.detectChanges();

    expect(component['dialogVisible']()).toBe(true);
    // The dialog is appended to the document body.
    const rows = document.querySelectorAll('.row-audit-table tbody tr');
    expect(rows.length).toBe(3);

    const trailText = document.querySelector('.row-audit-table')?.textContent ?? '';
    expect(trailText).toContain('alice');
    expect(trailText).toContain('carol');
    expect(trailText).toContain('bob');
    // First row is the newest change.
    expect(rows[0].textContent).toContain('alice');
  });

  it('shows a neutral "no history" state when there is no history', () => {
    const { fixture, component } = setup([]);

    expect(component['latest']()).toBeNull();
    const badgeText = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(badgeText).toContain('no history');

    component.open();
    fixture.detectChanges();
    expect(component['entries']().length).toBe(0);
    const emptyText = document.querySelector('.row-audit-empty')?.textContent ?? '';
    expect(emptyText).toContain('no history yet');
  });

  it('does not fetch when there is no record yet (falsy pkid)', () => {
    const { component, serviceSpy } = setup([], 0);

    expect(serviceSpy.getForRecord).not.toHaveBeenCalled();
    expect(component['entries']()).toEqual([]);
    expect(component['latest']()).toBeNull();
  });
});
