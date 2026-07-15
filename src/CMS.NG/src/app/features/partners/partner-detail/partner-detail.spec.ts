import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { PartnerDetail } from './partner-detail';
import { PartnerService } from '@core/services/partner.service';
import { Partner } from '@core/models/partner.model';

const partner: Partner = {
  pkid: 1,
  name: '恆逸',
  appKey: 'UUU',
  nameOnPartnerMenu: '恆逸教育訓練中心',
  nameOnCourseDetailPage: '恆逸',
  displayOrder: 1,
  imageFilename: 'uuu.png',
};

describe('PartnerDetail', () => {
  let fixture: ComponentFixture<PartnerDetail>;
  let component: PartnerDetail;
  let serviceSpy: jasmine.SpyObj<PartnerService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<PartnerService>('PartnerService', ['getById']);
    serviceSpy.getById.and.returnValue(of(partner));

    await TestBed.configureTestingModule({
      imports: [PartnerDetail],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: PartnerService, useValue: serviceSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PartnerDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the partner by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith(1);
    expect(component['partner']()?.name).toBe('恆逸');
  });

  it('renders the partner fields', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('恆逸');
    expect(text).toContain('UUU');
  });

  it('edit navigates to the edit route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/partners', 1, 'edit']);
  });
});
