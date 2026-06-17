import { Injectable, signal } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

export interface BreadcrumbItem {
  label: string;
  url: string | null;
}

interface RouteBreadcrumb {
  label: string;
  link?: string;
}

/**
 * Builds a breadcrumb trail and a page title from the active route.
 * Routes declare their own trail with:
 *   data: { title: 'Edit', breadcrumbs: [{ label: 'Roles', link: '/app/roles' }, { label: 'Edit' }] }
 */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  readonly breadcrumbs = signal<BreadcrumbItem[]>([]);
  readonly pageTitle = signal<string>('');

  constructor(private readonly router: Router) {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.rebuild());

    this.rebuild();
  }

  private rebuild(): void {
    let node: ActivatedRouteSnapshot | null = this.router.routerState.snapshot.root;
    let deepest: ActivatedRouteSnapshot | null = node;

    while (node) {
      if (Object.keys(node.data).length > 0) {
        deepest = node;
      }
      node = node.firstChild;
    }

    const data = deepest?.data ?? {};
    const trail = (data['breadcrumbs'] as RouteBreadcrumb[] | undefined) ?? [];
    const title = (data['title'] as string | undefined) ?? '';

    const items: BreadcrumbItem[] = trail.map((entry, index) => ({
      label: entry.label,
      url: index === trail.length - 1 ? null : entry.link ?? null
    }));

    this.breadcrumbs.set(items);
    this.pageTitle.set(title || (items.length ? items[items.length - 1].label : ''));
  }
}
