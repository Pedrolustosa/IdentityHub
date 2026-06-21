import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'searchHighlight',
  standalone: true
})
export class SearchHighlightPipe implements PipeTransform {
  /**
   * Highlights search terms in text with HTML markup.
   *
   * Usage:
   * {{ text | searchHighlight:searchTerm }}
   *
   * Example:
   * {{ 'Hello World' | searchHighlight:'world' }}
   * Output: Hello <mark>World</mark>
   */
  transform(text: string, searchTerm: string | null): string {
    if (!text || !searchTerm) {
      return text;
    }

    const escapedSearchTerm = this.escapeRegex(searchTerm);
    const regex = new RegExp(`(${escapedSearchTerm})`, 'gi');
    const highlighted = text.replace(regex, '<mark class="bg-warning-200 font-semibold">$1</mark>');

    return highlighted;
  }

  private escapeRegex(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }
}
