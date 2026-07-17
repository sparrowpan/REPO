import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { PartnerList } from './partner-list';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

describe('PartnerList', () => {
  let fixture: ComponentFixture<PartnerList>;
  let component: PartnerList;
  let serviceSpy: jasmine.SpyObj<PartnerService>;

  const partners: Partner[] = [
    { pkid: 1, name: '恆逸', appKey: 'UUU', nameOnPartnerMenu: '恆逸教育訓練中心', nameOnCourseDetailPage: '恆逸', displayOrder: 1, imageFilename: 'uuu.png' },
    { pkid: 2, name: '巨匠', appKey: 'PCS', nameOnPartnerMenu: '巨匠電腦', nameOnCourseDetailPage: '巨匠', displayOrder: 2, imageFilename: null },
  ];

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(partners));
    serviceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [PartnerList],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: PartnerService, useValue: serviceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PartnerList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads partners on init via query()', () => {
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['partners']().length).toBe(2);
  });

  it('renders a row per partner', () => {
    const rows = (fixture.nativeElement as HTMLElement).querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('applyFilters persists filters to sessionStorage and reloads', () => {
    component['filters'] = { keyword: '恆逸' };
    component.applyFilters();
    expect(sessionStorage.getItem('partner-list-filters')).toContain('恆逸');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('clearFilters resets filters and removes the stored key', () => {
    sessionStorage.setItem('partner-list-filters', '{"keyword":"x"}');
    component.clearFilters();
    expect(sessionStorage.getItem('partner-list-filters')).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('view navigates to the detail route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.view(partners[0]);
    expect(navSpy).toHaveBeenCalledWith(['/partners', 1]);
  });

  it('confirmDelete accepting deletes the partner and reloads', () => {
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    component.confirmDelete(partners[1]);
    expect(serviceSpy.delete).toHaveBeenCalledWith(2);
  });

  it('surfaces a load error without throwing', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBe(false);
  });
});
