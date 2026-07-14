import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes)],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the UWA brand', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.topbar__brand-text')?.textContent).toContain('UWA');
  });

  it('should render the 系統管理 Admin nav group with the 角色 AppRole child', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('系統管理 Admin');
    expect(text).toContain('角色 AppRole');
  });

  it('should toggle sidebar collapse', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance as unknown as { collapsed: () => boolean; toggleCollapse: () => void };
    expect(app.collapsed()).toBe(false);
    app.toggleCollapse();
    expect(app.collapsed()).toBe(true);
  });
});
