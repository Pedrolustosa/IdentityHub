import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { BirthdayGreetingService } from '../../core/services/birthday-greeting.service';
import { SidebarComponent } from '../../shared/components/sidebar/sidebar.component';
import { TopNavbarComponent } from '../../shared/components/top-navbar/top-navbar.component';
import { BreadcrumbsComponent } from '../../shared/components/breadcrumbs/breadcrumbs.component';
import { BirthdayCelebrationComponent } from '../../shared/components/birthday-celebration/birthday-celebration.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    TopNavbarComponent,
    BreadcrumbsComponent,
    BirthdayCelebrationComponent
  ],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css'
})
export class MainLayoutComponent implements OnInit {
  isSidebarCollapsed = false;
  isMobileSidebarOpen = false;

  constructor(private readonly birthdayGreeting: BirthdayGreetingService) {}

  ngOnInit(): void {
    this.birthdayGreeting.evaluateOnAppEntry();
  }

  toggleSidebar(): void {
    if (typeof window !== 'undefined' && window.matchMedia('(max-width: 1023px)').matches) {
      this.isMobileSidebarOpen = !this.isMobileSidebarOpen;
      return;
    }
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }

  closeMobileSidebar(): void {
    this.isMobileSidebarOpen = false;
  }
}
