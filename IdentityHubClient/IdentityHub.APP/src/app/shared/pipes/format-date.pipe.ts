import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'formatDate',
  standalone: true
})
export class FormatDatePipe implements PipeTransform {
  /**
   * Formats a date to a human-readable string or relative time.
   *
   * Usage:
   * {{ date | formatDate }}  // Default: "Jun 21, 2026"
   * {{ date | formatDate:'long' }}  // "June 21, 2026 at 3:45 PM"
   * {{ date | formatDate:'short' }}  // "6/21/26"
   * {{ date | formatDate:'time' }}  // "3:45 PM"
   * {{ date | formatDate:'relative' }}  // "2 hours ago", "in 3 days"
   */
  transform(value: any, format: string = 'default'): string {
    if (!value) return '';

    const date = new Date(value);
    if (isNaN(date.getTime())) return String(value);

    switch (format) {
      case 'long':
        return this.formatLong(date);
      case 'short':
        return this.formatShort(date);
      case 'time':
        return this.formatTime(date);
      case 'relative':
        return this.formatRelative(date);
      case 'default':
      default:
        return this.formatDefault(date);
    }
  }

  private formatDefault(date: Date): string {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return `${months[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`;
  }

  private formatLong(date: Date): string {
    const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
                        'July', 'August', 'September', 'October', 'November', 'December'];
    const time = this.formatTime(date);
    return `${monthNames[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()} at ${time}`;
  }

  private formatShort(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const year = String(date.getFullYear()).slice(-2);
    return `${month}/${day}/${year}`;
  }

  private formatTime(date: Date): string {
    const hours = String(date.getHours() % 12 || 12).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const ampm = date.getHours() >= 12 ? 'PM' : 'AM';
    return `${hours}:${minutes} ${ampm}`;
  }

  private formatRelative(date: Date): string {
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSecs = Math.floor(diffMs / 1000);
    const diffMins = Math.floor(diffSecs / 60);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);
    const diffWeeks = Math.floor(diffDays / 7);
    const diffMonths = Math.floor(diffDays / 30);
    const diffYears = Math.floor(diffDays / 365);

    if (diffMs < 0) {
      // Future date
      const absDiffSecs = Math.floor(-diffMs / 1000);
      const absDiffMins = Math.floor(absDiffSecs / 60);
      const absDiffHours = Math.floor(absDiffMins / 60);
      const absDiffDays = Math.floor(absDiffHours / 24);
      const absDiffWeeks = Math.floor(absDiffDays / 7);
      const absDiffMonths = Math.floor(absDiffDays / 30);
      const absDiffYears = Math.floor(absDiffDays / 365);

      if (absDiffYears > 0) return `in ${absDiffYears} year${absDiffYears > 1 ? 's' : ''}`;
      if (absDiffMonths > 0) return `in ${absDiffMonths} month${absDiffMonths > 1 ? 's' : ''}`;
      if (absDiffWeeks > 0) return `in ${absDiffWeeks} week${absDiffWeeks > 1 ? 's' : ''}`;
      if (absDiffDays > 0) return `in ${absDiffDays} day${absDiffDays > 1 ? 's' : ''}`;
      if (absDiffHours > 0) return `in ${absDiffHours} hour${absDiffHours > 1 ? 's' : ''}`;
      if (absDiffMins > 0) return `in ${absDiffMins} minute${absDiffMins > 1 ? 's' : ''}`;
      return 'in a few seconds';
    }

    if (diffSecs < 60) return 'just now';
    if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    if (diffWeeks < 4) return `${diffWeeks} week${diffWeeks > 1 ? 's' : ''} ago`;
    if (diffMonths < 12) return `${diffMonths} month${diffMonths > 1 ? 's' : ''} ago`;
    return `${diffYears} year${diffYears > 1 ? 's' : ''} ago`;
  }
}
