import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BreadcrumbService } from '../../../core/services/breadcrumb.service';

@Component({
  selector: 'app-breadcrumbs',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './breadcrumbs.component.html'
})
export class BreadcrumbsComponent {
  private readonly breadcrumbService = inject(BreadcrumbService);

  readonly items = this.breadcrumbService.breadcrumbs;
  readonly pageTitle = this.breadcrumbService.pageTitle;
}
