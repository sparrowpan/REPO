import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { PublishStatusDetail } from './publish-status-detail';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const status: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

describe('PublishStatusDetail', () => {
  let fixture: ComponentFixture<PublishStatusDetail>;
  let component: PublishStatusDetail;
  let serviceSpy: jasmine.SpyObj<PublishStatusService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', ['getById']);
    serviceSpy.getById.and.returnValue(of(status));

    await TestBed.configureTestingModule({
      imports: [PublishStatusDetail],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PublishStatusService, useValue: serviceSpy },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: '2' }) } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublishStatusDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the status by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['status']()?.description).toBe('已發布');
  });

  it('renders the status fields', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('已發布');
  });

  it('edit navigates to the edit route', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate');
    component.edit();
    expect(navSpy).toHaveBeenCalledWith(['/publish-statuses', 2, 'edit']);
  });
});
