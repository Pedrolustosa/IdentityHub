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

  it('stores access token in localStorage when rememberMe is true', () => {
    service.saveAccessToken('access', true);

    expect(localStorage.getItem('accessToken')).toBe('access');
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });

  it('stores access token in sessionStorage when rememberMe is false', () => {
    service.saveAccessToken('access', false);

    expect(sessionStorage.getItem('accessToken')).toBe('access');
    expect(localStorage.getItem('accessToken')).toBeNull();
  });

  it('clears all token storage entries', () => {
    localStorage.setItem('accessToken', 'a');
    sessionStorage.setItem('accessToken', 'a2');

    service.clearAll();

    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('accessToken')).toBeNull();
  });
});
