import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Course } from '@core/models/course.model';
import { AuthService } from '@core/services/auth.service';
import { CourseBrochurePrint } from './course-brochure-print';

/**
 * T6 — a **complete** Course. Every one of the 33 fields is populated.
 *
 * `course-detail.spec.ts` mocks with `as unknown as Course` and leaves 19 fields undefined; a
 * brochure bound to that fixture would render `undefined` for 時數 and 課程大綱 while the suite
 * stayed green. The cast is the bug — do not copy it here.
 */
const completeCourse: Course = {
  pkid: 42,
  title: 'AZ-900 Microsoft Azure 基礎課程',
  officialTitle: 'Microsoft Azure Fundamentals',
  courseId: 'AZ900',
  prodCourseId: 'PROD-AZ900',
  friendlyUrl: 'az-900',
  displayOrder: 7,
  partnerPkid: 1,
  courseGroupPkid: 3,
  publishStatusPkid: 20,
  scheduleOn: '2026-01-01',
  scheduleOff: '2030-12-31',
  hour: 14,
  listPrice: 12000,
  learningCredit: 3.5,
  material: '原廠電子教材',
  objective: '理解雲端運算概念\n熟悉 Azure 核心服務',
  target: '初次接觸雲端的 IT 人員',
  prerequisites: '無先修條件',
  outline: '<strong>Module 1</strong><br /><ul><li>雲端概念</li></ul>',
  towardCertOrExam: 'AZ-900 認證考試',
  note: '內部備註請勿外流',
  otherInfo: '其他內部資訊',
  canRepeat: true,
  partner: { pkid: 1, name: '微軟' },
  courseGroup: { pkid: 3, description: '雲端課程' },
  publishStatus: { pkid: 20, description: '已上架' },
  jobCategoryCount: 1,
  certificationCount: 1,
  jobCategoryPkids: [1],
  certificationPkids: [7],
  jobCategories: [{ pkid: 1, description: '雲端工程師' }],
  certifications: [{ pkid: 7, title: 'Azure Fundamentals' }],
};

function courseWith(overrides: Partial<Course>): Course {
  return { ...completeCourse, ...overrides };
}

describe('CourseBrochurePrint', () => {
  let fixture: ComponentFixture<CourseBrochurePrint>;
  let element: HTMLElement;

  async function render(course: Course): Promise<void> {
    fixture.componentRef.setInput('course', course);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  /** The document's section headings — what a reader scans. */
  function headings(): string[] {
    return [...element.querySelectorAll('.brochure__heading')].map(
      (h) => h.textContent?.trim() ?? '',
    );
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CourseBrochurePrint],
      providers: [{ provide: AuthService, useValue: { userName: signal('王小明') } }],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseBrochurePrint);
    element = fixture.nativeElement as HTMLElement;
  });

  // T1
  it('renders every field in the content contract', async () => {
    await render(completeCourse);
    const text = element.textContent ?? '';

    expect(text).toContain('AZ-900 Microsoft Azure 基礎課程'); // title
    expect(text).toContain('Microsoft Azure Fundamentals'); // officialTitle
    expect(text).toContain('微軟'); // partner.name
    expect(text).toContain('14'); // hour
    expect(text).toContain('Azure Fundamentals'); // certifications[]
    expect(text).toContain('雲端工程師'); // jobCategories[]
    expect(text).toContain('初次接觸雲端的 IT 人員'); // target
    expect(text).toContain('無先修條件'); // prerequisites
    expect(text).toContain('理解雲端運算概念'); // objective
    expect(text).toContain('雲端概念'); // outline
    expect(text).toContain('原廠電子教材'); // material
    expect(text).toContain('AZ-900 認證考試'); // towardCertOrExam
  });

  it('leads with the course title as the document heading', async () => {
    await render(completeCourse);
    expect(element.querySelector('h1')?.textContent?.trim()).toBe(
      'AZ-900 Microsoft Azure 基礎課程',
    );
  });

  it('excludes every admin field from the client document', async () => {
    await render(completeCourse);
    const text = element.textContent ?? '';

    expect(text).not.toContain('內部備註請勿外流'); // note
    expect(text).not.toContain('其他內部資訊'); // otherInfo
    expect(text).not.toContain('PROD-AZ900'); // prodCourseId
    expect(text).not.toContain('已上架'); // publishStatus
    expect(text).not.toContain('2030-12-31'); // scheduleOff
    expect(text).not.toContain('雲端課程'); // courseGroup
    expect(text).not.toContain('12000'); // listPrice — excluded pending a ruling
    expect(text).not.toContain('3.5'); // learningCredit — excluded pending a ruling
    expect(element.querySelector('row-audit-badge')).toBeNull();
  });

  // T2 — guards the nine `|| '—'` precedents in course-detail.html
  it('omits a section entirely when its field is absent, and never prints a placeholder', async () => {
    await render(courseWith({ objective: null, material: null, prerequisites: null }));

    // Scoped to the document, not the component: the screen-only warning banner names the missing
    // required fields on purpose, and it is hidden in print.
    const brochure = element.querySelector('.brochure')?.textContent ?? '';

    expect(brochure).not.toContain('課程目標');
    expect(brochure).not.toContain('教材');
    expect(brochure).not.toContain('先修條件');
    expect(brochure).not.toContain('—');
    expect(headings()).not.toContain('課程目標');
  });

  it('treats a whitespace-only field as absent', async () => {
    await render(courseWith({ target: '   \n  ' }));
    expect(headings()).not.toContain('適合對象');
  });

  it('keeps sections whose fields are present', async () => {
    await render(completeCourse);
    expect(element.textContent).toContain('課程目標');
    expect(element.textContent).toContain('課程大綱');
  });

  // T3
  it('warns the rep when a required field is missing', async () => {
    await render(courseWith({ outline: null }));

    const warning = element.querySelector('.brochure-warning');
    expect(warning).not.toBeNull();
    expect(warning?.textContent).toContain('課程大綱');
  });

  it('shows no warning when the brochure is complete', async () => {
    await render(completeCourse);
    expect(element.querySelector('.brochure-warning')).toBeNull();
  });

  // T7
  it('warns on every missing required field when the course is essentially empty', async () => {
    fixture.componentRef.setInput(
      'course',
      courseWith({
        objective: null,
        outline: null,
        target: null,
        prerequisites: null,
        material: null,
        towardCertOrExam: null,
        officialTitle: null,
      }),
    );
    fixture.detectChanges();

    expect(fixture.componentInstance.missingRequired()).toEqual(['課程目標', '課程大綱']);
    expect(element.querySelector('.brochure-warning')?.textContent).toContain('課程目標、課程大綱');
  });

  it('renders operator-authored markup as real structure, not as literal tags', async () => {
    await render(completeCourse);

    const outline = element.querySelector('.brochure__section--outline');
    expect(outline?.querySelector('strong')?.textContent).toBe('Module 1');
    expect(outline?.querySelector('li')?.textContent).toBe('雲端概念');
    expect(outline?.textContent).not.toContain('<strong>');
  });

  it('keeps line breaks in plain-text prose', async () => {
    await render(completeCourse);

    const objective = [...element.querySelectorAll('.brochure__prose')].find((el) =>
      el.textContent?.includes('理解雲端運算概念'),
    );
    expect(objective?.querySelectorAll('br').length).toBe(1);
  });

  it('sanitizes prose rather than trusting it', async () => {
    await render(courseWith({ objective: '<p>安全</p><script>alert(1)</script>' }));

    expect(element.querySelector('script')).toBeNull();
    expect(element.textContent).toContain('安全');
  });

  it('names the signed-in rep as the contact, from the session', async () => {
    await render(completeCourse);
    expect(element.querySelector('.brochure__contact-name')?.textContent?.trim()).toBe('王小明');
  });

  it('footers the public course URL so a printed copy can find the live page', async () => {
    await render(completeCourse);
    expect(element.querySelector('.brochure__latest')?.textContent).toContain(
      'https://www.uuu.com.tw/Course/Show/42/AZ900',
    );
  });
});
