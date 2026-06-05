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

  it('stores tokens in localStorage when rememberMe is true', () => {
    service.saveTokens('access', 'refresh', true);

    expect(localStorage.getItem('accessToken')).toBe('access');
    expect(localStorage.getItem('refreshToken')).toBe('refresh');
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('refreshToken')).toBeNull();
  });

  it('stores tokens in sessionStorage when rememberMe is false', () => {
    service.saveTokens('access', 'refresh', false);

    expect(sessionStorage.getItem('accessToken')).toBe('access');
    expect(sessionStorage.getItem('refreshToken')).toBe('refresh');
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('refreshToken')).toBeNull();
  });

  it('clears all token storage entries', () => {
    localStorage.setItem('accessToken', 'a');
    localStorage.setItem('refreshToken', 'r');
    sessionStorage.setItem('accessToken', 'a2');
    sessionStorage.setItem('refreshToken', 'r2');

    service.clearAll();

    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('refreshToken')).toBeNull();
  });
});
