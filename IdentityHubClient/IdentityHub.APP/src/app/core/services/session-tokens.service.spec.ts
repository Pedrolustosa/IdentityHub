import { TestBed } from '@angular/core/testing';
import { SessionTokensService } from './session-tokens.service';

describe('SessionTokensService', () => {
  let service: SessionTokensService;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();

    TestBed.configureTestingModule({});
    service = TestBed.inject(SessionTokensService);
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('keeps the access token in memory only when rememberMe is true', () => {
    service.saveAccessToken('access', true);

    expect(service.getAccessToken()).toBe('access');
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('ih_remember_me')).toBe('1');
  });

  it('keeps the access token in memory only when rememberMe is false', () => {
    service.saveAccessToken('access', false);

    expect(service.getAccessToken()).toBe('access');
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('ih_remember_me')).toBe('0');
  });

  it('updates the in-memory access token', () => {
    service.saveAccessToken('old', true);
    service.updateAccessToken('new');

    expect(service.getAccessToken()).toBe('new');
  });

  it('clears memory token, preference, and legacy storage entries', () => {
    service.saveAccessToken('access', true);
    localStorage.setItem('accessToken', 'legacy');
    sessionStorage.setItem('accessToken', 'legacy2');

    service.clearAll();

    expect(service.getAccessToken()).toBeNull();
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('ih_remember_me')).toBeNull();
  });

  it('removes legacy access tokens without clearing remember preference', () => {
    localStorage.setItem('ih_remember_me', '1');
    localStorage.setItem('accessToken', 'legacy');

    service.clearLegacyAccessTokens();

    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('ih_remember_me')).toBe('1');
  });
});
