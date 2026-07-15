import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { PublishStatusList } from './publish-status-list';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

describe('PublishStatusList', () => {
  let fixture: ComponentFixture<PublishStatusList>;
  let component: PublishStatusList;
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;

  const statuses: PublishStatus[] = [
    { pkid: 1, description: '草稿', isDraft: true, isPublished: false, isDiscontinued: false },
    { pkid: 2, description: '已發布', isDraft: false, isPublished: true, isDiscontinued: false },
  ];

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(statuses));
    serviceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [PublishStatusList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: PublishStatusService, useValue: serviceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublishStatusList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads statuses on init via query()', () => {
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['statuses']().length).toBe(2);
  });

  it('renders a row per status', () => {
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilters persists filters to sessionStorage and reloads', () => {
    component['filters'] = { keyword: '草稿', isDraft: null, isPublished: null, isDiscontinued: null };
    component.applyFilters();
    expect(sessionStorage.getItem('publishStatus-list-filters')).toContain('草稿');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('clearFilters resets filters and removes the stored key', () => {
    sessionStorage.setItem('publishStatus-list-filters', '{"keyword":"x"}');
    component.clearFilters();
    expect(sessionStorage.getItem('publishStatus-list-filters')).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.view(statuses[0]);
    expect(navSpy).toHaveBeenCalledWith(['/publish-statuses', 1]);
  });

  it('confirmDelete accepting deletes the status and reloads', () => {
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmDelete(statuses[1]);
    expect(serviceSpy.delete).toHaveBeenCalledWith(2);
  });

  it('surfaces a load error without throwing', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBe(false);
  });
});
