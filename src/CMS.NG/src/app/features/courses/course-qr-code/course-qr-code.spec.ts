import { ComponentFixture, TestBed } from '@angular/core/testing';
import QRCode from 'qrcode';

import { CourseQrCode } from './course-qr-code';

describe('CourseQrCode', () => {
  let fixture: ComponentFixture<CourseQrCode>;
  let component: CourseQrCode;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseQrCode],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseQrCode);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('pkid', 42);
    fixture.componentRef.setInput('courseId', 'AZ900');
  });

  it('encodes the public course URL built from pkid and courseId', async () => {
    const toDataUrl = spyOn(QRCode, 'toDataURL').and.callThrough();

    await component.render();

    expect(component.url()).toBe('https://www.uuu.com.tw/Course/Show/42/AZ900');
    expect(toDataUrl).toHaveBeenCalled();
    expect(toDataUrl.calls.mostRecent().args[0]).toBe(
      'https://www.uuu.com.tw/Course/Show/42/AZ900',
    );
  });

  it('shows the CourseId as the title', () => {
    fixture.detectChanges();
    const title = (fixture.nativeElement as HTMLElement).querySelector('.qr__title');
    expect(title?.textContent?.trim()).toBe('AZ900');
  });

  it('renders the QR code as a PNG image', async () => {
    await component.render();
    fixture.detectChanges();

    const img = (fixture.nativeElement as HTMLElement).querySelector<HTMLImageElement>('.qr__image');
    expect(img).not.toBeNull();
    expect(img!.src).toMatch(/^data:image\/png/);
  });

  it('download produces a PNG image named after the CourseId', async () => {
    await component.render();

    const anchor = document.createElement('a');
    const clickSpy = spyOn(anchor, 'click');
    spyOn(document, 'createElement').and.returnValue(anchor);

    component.download();

    expect(anchor.href).toMatch(/^data:image\/png/);
    expect(anchor.download).toBe('AZ900-qrcode.png');
    expect(clickSpy).toHaveBeenCalled();
  });

  it('download does nothing before the QR code has rendered', () => {
    const createSpy = spyOn(document, 'createElement');
    component.download();
    expect(createSpy).not.toHaveBeenCalled();
  });
});
