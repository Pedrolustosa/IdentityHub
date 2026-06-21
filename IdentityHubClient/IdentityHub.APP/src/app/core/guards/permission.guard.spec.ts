import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, ActivatedRouteSnapshot } from '@angular/router';
import { permissionGuard } from './permission.guard';
import { AuthService } from '../services/auth.service';

describe('permissionGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  function run(data: Record<string, unknown>): boolean | UrlTree {
    const route = { data } as unknown as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => permissionGuard(route, {} as never)) as boolean | UrlTree;
  }

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['isAuthenticated', 'hasPermission']);

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authServiceSpy }]
    });

    router = TestBed.inject(Router);
  });

  it('redirects to /login when not authenticated', () => {
    authServiceSpy.isAuthenticated.and.returnValue(false);

    const result = run({ permission: 'Users.View' });

    expect(result).toEqual(router.createUrlTree(['/login']));
  });

  it('allows access when there is no required permission', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);

    const result = run({});

    expect(result).toBeTrue();
  });

  it('allows access when the single required permission is present', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.hasPermission.and.callFake((p) => p === 'Users.View');

    const result = run({ permission: 'Users.View' });

    expect(result).toBeTrue();
  });

  it('allows access with any permission when requireAll is not set', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.hasPermission.and.callFake((p) => p === 'Audit.View');

    const result = run({ permissions: ['SecurityEvents.View', 'Audit.View'] });

    expect(result).toBeTrue();
  });

  it('requires all permissions when requireAll is true', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.hasPermission.and.callFake((p) => p === 'Roles.View');

    const result = run({ permissions: ['Roles.View', 'Roles.Permissions.View'], requireAll: true });

    expect(result).toEqual(router.createUrlTree(['/app/access-denied'], {
      queryParams: {
        required: 'Roles.View,Roles.Permissions.View'
      }
    }));
  });

  it('redirects to /app/access-denied when the required permission is missing', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.hasPermission.and.returnValue(false);

    const result = run({ permission: 'Users.View' });

    expect(result).toEqual(router.createUrlTree(['/app/access-denied'], {
      queryParams: {
        required: 'Users.View'
      }
    }));
  });
});
