import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { SidebarComponent } from './sidebar.component';
import { AuthService } from '../../../core/services/auth.service';
import { SecurityAlertsService } from '../../../features/security-alerts/security-alerts.service';

describe('SidebarComponent permission flow', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let securityAlertsServiceSpy: jasmine.SpyObj<SecurityAlertsService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    securityAlertsServiceSpy = jasmine.createSpyObj<SecurityAlertsService>('SecurityAlertsService', ['getSecurityAlerts']);
    securityAlertsServiceSpy.getSecurityAlerts.and.returnValue(of({
      items: [],
      page: 1,
      pageSize: 1,
      totalCount: 3,
      totalPages: 1
    }));

    await TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: SecurityAlertsService, useValue: securityAlertsServiceSpy }
      ]
    }).compileComponents();
  });

  it('shows new security/account menu entries when permissions are granted', () => {
    const granted = new Set([
      'Dashboard.View',
      'Users.View',
      'Roles.View',
      'SecurityEvents.View',
      'Audit.View',
      'Sessions.View',
      'Activity.View',
      'SecuritySettings.View',
      'Users.Invites.View',
      'Roles.Permissions.View',
      'SecurityEvents.Manage'
    ]);

    authServiceSpy.hasPermission.and.callFake((permission: string) => granted.has(permission));

    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const labels = Object.values(component.groupedMenuItems)
      .flat()
      .map((item) => item.label);

    expect(labels).toContain('System Sessions');
    expect(labels).toContain('Recent Activity');
    expect(labels).toContain('Security Settings');
    expect(labels).toContain('User Invites');
    expect(labels).toContain('Permissions Matrix');
    expect(labels).toContain('Permissions Catalog');
    expect(securityAlertsServiceSpy.getSecurityAlerts).toHaveBeenCalled();
  });

  it('hides restricted new menu entries for limited users', () => {
    const granted = new Set(['Dashboard.View']);

    authServiceSpy.hasPermission.and.callFake((permission: string) => granted.has(permission));

    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    const labels = Object.values(component.groupedMenuItems)
      .flat()
      .map((item) => item.label);

    expect(labels).not.toContain('System Sessions');
    expect(labels).not.toContain('Recent Activity');
    expect(labels).not.toContain('Security Settings');
    expect(labels).not.toContain('User Invites');
    expect(labels).not.toContain('Permissions Matrix');
    expect(labels).not.toContain('Permissions Catalog');
    expect(securityAlertsServiceSpy.getSecurityAlerts).not.toHaveBeenCalled();
  });
});
