import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { authRefreshInterceptor } from './auth-refresh.interceptor';
import { SessionTokensService } from '../services/session-tokens.service';
import { environment } from '../../../environments/environment';

describe('authRefreshInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let tokensSpy: jasmine.SpyObj<SessionTokensService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const refreshUrl = `${environment.apiUrl}/auth/refresh`;
  const targetUrl = '/api/protected';

  beforeEach(() => {
    tokensSpy = jasmine.createSpyObj<SessionTokensService>('SessionTokensService', [
      'getAccessToken',
      'updateAccessToken',
      'clearAll'
    ]);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authRefreshInterceptor])),
        provideHttpClientTesting(),
        { provide: SessionTokensService, useValue: tokensSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('refreshes the access token and retries the original request after a 401', fakeAsync(() => {
    let result: unknown = null;
    http.get(targetUrl).subscribe((res) => (result = res));

    httpMock.expectOne(targetUrl).flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    tick();

    httpMock.expectOne(refreshUrl).flush({ token: 'new-access', refreshToken: 'new-refresh' });
    tick();

    const retried = httpMock.expectOne(targetUrl);
    expect(retried.request.headers.get('Authorization')).toBe('Bearer new-access');
    retried.flush({ ok: true });
    tick();

    expect(tokensSpy.updateAccessToken).toHaveBeenCalledWith('new-access');
    expect(result).toEqual({ ok: true });
  }));

  it('clears the session and redirects to login when refresh fails', fakeAsync(() => {
    let errored = false;
    http.get(targetUrl).subscribe({
      next: () => undefined,
      error: () => (errored = true)
    });

    httpMock.expectOne(targetUrl).flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    tick();

    httpMock.expectOne(refreshUrl).flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    tick();

    expect(tokensSpy.clearAll).toHaveBeenCalled();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
    expect(errored).toBeTrue();
  }));
});
