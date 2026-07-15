import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService } from 'primeng/api';
import { of } from 'rxjs';

import { FeaturedPromoItemBoard } from './featured-promo-item-board';
import { FeaturedPromoItemService } from '@core/services/featured-promo-item.service';
import { LookupService } from '@core/services/lookup.service';
import { FeaturedPromoItem } from '@core/models/featured-promo-item.model';
import { TrainingCenterLookup } from '@core/models/training-center-lookup.model';
import { PromotionLookup } from '@core/models/promotion-lookup.model';
import { toIso } from '@core/utils/date.util';

const CENTERS: TrainingCenterLookup[] = [
  { pkid: 1, name: '台北' },
  { pkid: 2, name: '新竹' },
];

const PROMOTIONS: PromotionLookup[] = [
  { pkid: 101, promoCode: '20251204_SkillTrainAI', topic: '成為能AI協作的程式設計師', description: '轉職就業養成班' },
  { pkid: 102, promoCode: '251211_GoogleAI', topic: 'Google AI工具一次掌握', description: '不需技術基礎' },
];

interface Harness {
  fixture: ComponentFixture<FeaturedPromoItemBoard>;
  component: FeaturedPromoItemBoard;
  serviceSpy: jasmine.SpyObj<FeaturedPromoItemService>;
  lookupSpy: jasmine.SpyObj<LookupService>;
  currentItems: FeaturedPromoItem[];
}

function setup(): Harness {
  const currentItems: FeaturedPromoItem[] = [];

  const serviceSpy = jasmine.createSpyObj<FeaturedPromoItemService>('FeaturedPromoItemService', [
    'query',
    'create',
    'update',
    'delete',
    'move',
  ]);
  serviceSpy.query.and.callFake(() => of([...currentItems]));
  serviceSpy.create.and.returnValue(of({} as FeaturedPromoItem));
  serviceSpy.update.and.returnValue(of(void 0));
  serviceSpy.delete.and.returnValue(of(void 0));
  serviceSpy.move.and.returnValue(of(void 0));

  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', [
    'getTrainingCenters',
    'getPromotions',
  ]);
  lookupSpy.getTrainingCenters.and.returnValue(of(CENTERS));
  lookupSpy.getPromotions.and.returnValue(of(PROMOTIONS));

  TestBed.configureTestingModule({
    imports: [FeaturedPromoItemBoard],
    providers: [
      provideRouter([]),
      provideNoopAnimations(),
      { provide: FeaturedPromoItemService, useValue: serviceSpy },
      { provide: LookupService, useValue: lookupSpy },
    ],
  });

  const fixture = TestBed.createComponent(FeaturedPromoItemBoard);
  fixture.detectChanges(); // triggers ngOnInit
  return { fixture, component: fixture.componentInstance, serviceSpy, lookupSpy, currentItems };
}

/** First day (Monday) of the component's active week. */
function monday(component: FeaturedPromoItemBoard): Date {
  return component['weekDays']()[0];
}

/** Seed items for the active week and reload so itemAt() can find them. */
function seedItem(h: Harness, slot: number, over: Partial<FeaturedPromoItem> = {}): FeaturedPromoItem {
  const item: FeaturedPromoItem = {
    pkid: 10 + slot,
    scheduleOn: toIso(monday(h.component))!,
    trainingCenterPkid: 1,
    slot,
    promotionPkid: 101,
    promoCode: '20251204_SkillTrainAI',
    topic: '成為能AI協作的程式設計師',
    description: '轉職就業養成班',
    ...over,
  };
  h.currentItems.push(item);
  h.component.loadItems();
  h.fixture.detectChanges();
  return item;
}

describe('FeaturedPromoItemBoard — init & rendering', () => {
  it('loads centers, promotions and items on init', () => {
    const { component, serviceSpy, lookupSpy } = setup();
    expect(lookupSpy.getTrainingCenters).toHaveBeenCalled();
    expect(lookupSpy.getPromotions).toHaveBeenCalled();
    expect(serviceSpy.query).toHaveBeenCalled();
    expect(component['centers']().length).toBe(2);
    expect(component['activeCenter']()).toBe(1);
  });

  it('renders a tab per training center', () => {
    const { fixture } = setup();
    const tabs = (fixture.nativeElement as HTMLElement).querySelectorAll('.tab');
    expect(tabs.length).toBe(2);
  });

  it('renders 7 day blocks with 3 slots each', () => {
    const { fixture } = setup();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelectorAll('.day').length).toBe(7);
    expect(el.querySelectorAll('.slotrow').length).toBe(21);
  });

  it('queries with the active center and the Monday–Sunday range', () => {
    const { component, serviceSpy } = setup();
    const days = component['weekDays']();
    expect(serviceSpy.query).toHaveBeenCalledWith({
      trainingCenterPkid: 1,
      scheduleOnFrom: toIso(days[0]),
      scheduleOnTo: toIso(days[6]),
    });
  });
});

describe('FeaturedPromoItemBoard — tabs & week navigation', () => {
  it('selectCenter switches the active tab and reloads', () => {
    const { component, serviceSpy } = setup();
    component.selectCenter(2);
    expect(component['activeCenter']()).toBe(2);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('selectCenter on the current tab does nothing', () => {
    const { component, serviceSpy } = setup();
    component.selectCenter(1);
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
  });

  it('nextWeek advances the week by 7 days and reloads', () => {
    const { component, serviceSpy } = setup();
    const before = monday(component).getTime();
    component.nextWeek();
    expect(monday(component).getTime()).toBe(before + 7 * 86_400_000);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('prevWeek moves the week back by 7 days', () => {
    const { component } = setup();
    const before = monday(component).getTime();
    component.prevWeek();
    expect(monday(component).getTime()).toBe(before - 7 * 86_400_000);
  });
});

describe('FeaturedPromoItemBoard — Edit existing', () => {
  it('startEdit populates the form from the existing item', () => {
    const h = setup();
    seedItem(h, 1);
    h.component.startEdit(monday(h.component), 1);
    expect(h.component.isEditing(monday(h.component), 1)).toBe(true);
    expect(h.component['editModel'].promoCode).toBe('20251204_SkillTrainAI');
    expect(h.component['editModel'].pkid).toBe(11);
  });

  it('save in edit mode PUTs the item', () => {
    const h = setup();
    seedItem(h, 1);
    h.component.startEdit(monday(h.component), 1);
    h.component.save(monday(h.component), 1);
    expect(h.serviceSpy.update).toHaveBeenCalled();
    expect(h.serviceSpy.create).not.toHaveBeenCalled();
    const req = h.serviceSpy.update.calls.mostRecent().args[0];
    expect(req.pkid).toBe(11);
    expect(req.slot).toBe(1);
  });
});

describe('FeaturedPromoItemBoard — New / empty cell', () => {
  it('startEdit on an empty cell opens a blank form', () => {
    const { component } = setup();
    component.startEdit(monday(component), 2);
    expect(component['editModel'].pkid).toBe(0);
    expect(component['editModel'].promoCode).toBe('');
  });

  it('resolvePromoCode sets promotionPkid and pre-fills topic/description', () => {
    const { component } = setup();
    component.startEdit(monday(component), 2);
    component['editModel'].promoCode = '251211_GoogleAI';
    const match = component.resolvePromoCode();
    expect(match?.pkid).toBe(102);
    expect(component['editModel'].promotionPkid).toBe(102);
    expect(component['editModel'].topic).toBe('Google AI工具一次掌握');
    expect(component['editModel'].description).toBe('不需技術基礎');
  });

  it('save creates a new item at the cell coordinates', () => {
    const { component, serviceSpy } = setup();
    const day = monday(component);
    component.startEdit(day, 3);
    component['editModel'].promoCode = '251211_GoogleAI';
    component.save(day, 3);
    expect(serviceSpy.create).toHaveBeenCalled();
    const req = serviceSpy.create.calls.mostRecent().args[0];
    expect(req.pkid).toBe(0);
    expect(req.slot).toBe(3);
    expect(req.trainingCenterPkid).toBe(1);
    expect(req.scheduleOn).toBe(toIso(day)!);
    expect(req.promotionPkid).toBe(102);
  });

  it('save does nothing when the PromoCode does not resolve', () => {
    const { component, serviceSpy } = setup();
    component.startEdit(monday(component), 1);
    component['editModel'].promoCode = 'DOES_NOT_EXIST';
    component.save(monday(component), 1);
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(serviceSpy.update).not.toHaveBeenCalled();
  });

  it('cancelEdit closes the form', () => {
    const { component } = setup();
    component.startEdit(monday(component), 1);
    component.cancelEdit();
    expect(component.isEditing(monday(component), 1)).toBe(false);
  });
});

describe('FeaturedPromoItemBoard — Copy / Paste', () => {
  it('copy stores the cell payload on the clipboard', () => {
    const h = setup();
    const item = seedItem(h, 1);
    h.component.copy(item);
    expect(h.component['clipboard']()?.promoCode).toBe(item.promoCode);
  });

  it('paste opens a new-item form pre-filled from the clipboard', () => {
    const h = setup();
    const item = seedItem(h, 1);
    h.component.copy(item);
    const day = monday(h.component);
    h.component.startPaste(day, 3);
    expect(h.component.isEditing(day, 3)).toBe(true);
    expect(h.component['editModel'].pkid).toBe(0);
    expect(h.component['editModel'].promoCode).toBe(item.promoCode);
    expect(h.component['editModel'].topic).toBe(item.topic);
  });

  it('paste does nothing when the clipboard is empty', () => {
    const { component } = setup();
    component.startPaste(monday(component), 1);
    expect(component.isEditing(monday(component), 1)).toBe(false);
  });
});

describe('FeaturedPromoItemBoard — Delete & Move', () => {
  it('confirmDelete accepting deletes the item and reloads', () => {
    const h = setup();
    const item = seedItem(h, 1);
    const confirmation = h.fixture.debugElement.injector.get(ConfirmationService);
    spyOn(confirmation, 'confirm').and.callFake((opts) => {
      opts.accept?.();
      return confirmation;
    });
    h.component.confirmDelete(item);
    expect(h.serviceSpy.delete).toHaveBeenCalledWith(item.pkid);
  });

  it('moveDown moves the item to the next slot', () => {
    const h = setup();
    const item = seedItem(h, 1);
    h.component.moveDown(item);
    expect(h.serviceSpy.move).toHaveBeenCalledWith({ pkid: item.pkid, targetSlot: 2 });
  });

  it('moveDown at slot 3 is a no-op', () => {
    const h = setup();
    const item = seedItem(h, 3);
    h.component.moveDown(item);
    expect(h.serviceSpy.move).not.toHaveBeenCalled();
  });

  it('moveUp moves the item to the previous slot', () => {
    const h = setup();
    const item = seedItem(h, 2);
    h.component.moveUp(item);
    expect(h.serviceSpy.move).toHaveBeenCalledWith({ pkid: item.pkid, targetSlot: 1 });
  });

  it('moveUp at slot 1 is a no-op', () => {
    const h = setup();
    const item = seedItem(h, 1);
    h.component.moveUp(item);
    expect(h.serviceSpy.move).not.toHaveBeenCalled();
  });
});
