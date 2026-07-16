import { escapeHtml, hasMarkup, toProseHtml } from './prose-html.util';

describe('prose-html.util', () => {
  describe('hasMarkup', () => {
    it('detects the tags the catalogue actually contains', () => {
      expect(hasMarkup('第一天<br />第二天')).toBeTrue();
      expect(hasMarkup('<strong>Module 1</strong>')).toBeTrue();
      expect(hasMarkup('<ul><li>Azure</li></ul>')).toBeTrue();
      expect(hasMarkup('<a href="https://www.uuu.com.tw">詳細資訊</a>')).toBeTrue();
      expect(hasMarkup('<p>段落</p>')).toBeTrue();
    });

    it('does not mistake prose using angle brackets for markup', () => {
      expect(hasMarkup('時數 < 8 小時')).toBeFalse();
      expect(hasMarkup('a > b')).toBeFalse();
      expect(hasMarkup('第一天\n第二天')).toBeFalse();
    });
  });

  describe('escapeHtml', () => {
    it('escapes the five markup characters', () => {
      expect(escapeHtml(`<a href="x">&'`)).toBe('&lt;a href=&quot;x&quot;&gt;&amp;&#39;');
    });

    it('replaces & first so the other escapes are not double-escaped', () => {
      expect(escapeHtml('<')).toBe('&lt;');
      expect(escapeHtml('&lt;')).toBe('&amp;lt;');
    });
  });

  describe('toProseHtml', () => {
    it('returns null for absent or blank values', () => {
      expect(toProseHtml(null)).toBeNull();
      expect(toProseHtml(undefined)).toBeNull();
      expect(toProseHtml('')).toBeNull();
      expect(toProseHtml('   \n\t  ')).toBeNull();
    });

    it('passes markup through untouched for the sanitizer to handle', () => {
      const outline = '<strong>Module 1</strong><br /><ul><li>Azure fundamentals</li></ul>';
      expect(toProseHtml(outline)).toBe(outline);
    });

    it('converts newlines to <br> so plain text keeps its structure', () => {
      expect(toProseHtml('第一天\n第二天')).toBe('第一天<br>第二天');
      expect(toProseHtml('第一天\r\n第二天')).toBe('第一天<br>第二天');
    });

    it('escapes plain text, so prose using angle brackets cannot become markup', () => {
      expect(toProseHtml('時數 < 8 小時\n价格 > 0')).toBe('時數 &lt; 8 小時<br>价格 &gt; 0');
    });

    // The two branches have different safety stories and it matters which one owns what: plain text
    // is made safe *here* by escaping, while anything carrying tags is passed through for Angular's
    // sanitizer to strip at bind time. Neither branch trusts the operator.
    it('leaves a tag-bearing value for the sanitizer rather than escaping it into visible junk', () => {
      const dangerous = '<p>安全</p><script>alert(1)</script>';
      expect(toProseHtml(dangerous)).toBe(dangerous);
      // course-brochure-print.spec.ts proves the <script> never survives the bind.
    });

    it('keeps short but real values — a length rule would delete most of Material', () => {
      // 639 of 1,076 Material values are under ten characters; every one is legitimate.
      expect(toProseHtml('上課講義')).toBe('上課講義');
      expect(toProseHtml('教材')).toBe('教材');
    });

    it('trims surrounding whitespace', () => {
      expect(toProseHtml('  課程大綱  ')).toBe('課程大綱');
    });
  });
});
