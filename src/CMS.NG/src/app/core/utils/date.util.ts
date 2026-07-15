/**
 * Date <-> ISO helpers for form binding.
 *
 * Always uses LOCAL date components — never `toISOString()`, which converts to UTC
 * first and shifts the day for UTC+8 users (off-by-one on ScheduleOn/ScheduleOff).
 */

/** Serialize a Date to a local `yyyy-MM-dd` string (or null). */
export function toIso(d: Date | null | undefined): string | null {
  if (!d) return null;
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Parse an ISO `yyyy-MM-dd` string into a local Date (or null). */
export function fromIso(s: string | null | undefined): Date | null {
  if (!s) return null;
  const [y, m, d] = s.split('T')[0].split('-').map(Number);
  if (!y || !m || !d) return null;
  return new Date(y, m - 1, d);
}
