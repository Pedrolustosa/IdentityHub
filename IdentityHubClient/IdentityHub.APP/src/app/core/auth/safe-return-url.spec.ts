import { resolveSafeReturnUrl } from './safe-return-url';

describe('resolveSafeReturnUrl', () => {
  it('returns the fallback for missing or unsafe values', () => {
    expect(resolveSafeReturnUrl(null)).toBe('/app/dashboard');
    expect(resolveSafeReturnUrl('https://evil.example/phish')).toBe('/app/dashboard');
    expect(resolveSafeReturnUrl('//evil.example')).toBe('/app/dashboard');
    expect(resolveSafeReturnUrl('app/users')).toBe('/app/dashboard');
  });

  it('accepts in-app absolute paths', () => {
    expect(resolveSafeReturnUrl('/app/users/u1')).toBe('/app/users/u1');
    expect(resolveSafeReturnUrl('/app/sessions?page=2')).toBe('/app/sessions?page=2');
  });
});
