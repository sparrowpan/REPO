/**
 * Operator-authored prose (課程大綱, 課程目標, 適合對象, …) is stored as free text, but it is not all
 * the same kind of free text. Measured against the live catalogue (1,085 courses), roughly one row
 * in eight carries benign HTML — `<br>`, `<strong>`, `<a>`, `<li>`, `<p>` — while the rest is plain
 * text whose only structure is newlines.
 *
 * Rendering either one the other's way is wrong:
 *
 * - `white-space: pre-wrap` (what `course-detail` does) prints the markup rows' raw tags. Harmless
 *   in an admin view; unacceptable in a document that goes to a client.
 * - `[innerHTML]` alone collapses the plain rows' line breaks, because HTML ignores `\n`.
 *
 * {@link toProseHtml} normalizes both into HTML safe to bind with `[innerHTML]`. Angular's default
 * `DomSanitizer` strips anything dangerous at bind time, and the catalogue contains no `<script>`,
 * `<img>`, `<table>` or `<span>` — so nothing legitimate is lost to sanitization.
 *
 * **Never pair this with `bypassSecurityTrustHtml`.** The sanitizer is the entire safety story here;
 * bypassing it would turn an operator-editable column into stored XSS.
 */

/** An `<a`, `<br/>`, `<strong>`… — a letter after `<` is what separates markup from prose using `<`. */
const MARKUP_PATTERN = /<[a-z][\s\S]*>/i;

/** True when `value` looks like it carries HTML rather than plain text. */
export function hasMarkup(value: string): boolean {
  return MARKUP_PATTERN.test(value);
}

/**
 * Escape the five characters that would otherwise be read as markup. `&` must be replaced first, or
 * the ampersands introduced by the later replacements get double-escaped.
 */
export function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

/**
 * Normalize an operator-authored prose field into HTML for `[innerHTML]`, or `null` when the field
 * has nothing to say.
 *
 * `null` is the signal to omit the section entirely — a brochure must never print a `—` placeholder
 * at a client (`course-detail.html` does that nine times; it is an admin idiom, not a document one).
 *
 * Absence is tested by content, never by length. 639 of 1,076 `Material` values are shorter than ten
 * characters and every one of them is real, so a minimum-length rule would silently delete most of
 * that column. Five courses do hold literal placeholder junk in `Outline` ("Test", "string", "00");
 * that is a data-quality problem for the CMS team and is deliberately not guessed at here.
 */
export function toProseHtml(value: string | null | undefined): string | null {
  if (value == null) return null;
  const trimmed = value.trim();
  if (!trimmed) return null;
  return hasMarkup(trimmed) ? trimmed : escapeHtml(trimmed).replace(/\r?\n/g, '<br>');
}
