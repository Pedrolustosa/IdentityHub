/** Shared birthday date helpers (local timezone). */

export type DateOnlyParts = { year: number; month: number; day: number };

export function parseDateOnly(value: string | null | undefined): DateOnlyParts | null {
  if (!value) {
    return null;
  }
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value.trim());
  if (!match) {
    return null;
  }
  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  if (!year || month < 1 || month > 12 || day < 1 || day > 31) {
    return null;
  }
  return { year, month, day };
}

function isLeapYear(year: number): boolean {
  return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
}

export function isBirthdayOnDate(dob: DateOnlyParts, date: Date = new Date()): boolean {
  const month = date.getMonth() + 1;
  const day = date.getDate();

  if (dob.month === month && dob.day === day) {
    return true;
  }

  // Feb 29 → celebrate on Feb 28 in non-leap years
  if (dob.month === 2 && dob.day === 29 && month === 2 && day === 28 && !isLeapYear(date.getFullYear())) {
    return true;
  }

  return false;
}

export function isDateOfBirthToday(value: string | null | undefined, date: Date = new Date()): boolean {
  const dob = parseDateOnly(value);
  return dob != null && isBirthdayOnDate(dob, date);
}

export function turningAgeFromDateOfBirth(
  value: string | null | undefined,
  date: Date = new Date()
): number | null {
  const dob = parseDateOnly(value);
  if (!dob) {
    return null;
  }
  const age = date.getFullYear() - dob.year;
  return age > 0 && age < 130 ? age : null;
}
