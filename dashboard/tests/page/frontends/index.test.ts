import '@testing-library/jest-dom';
import { FrontendsListingPage } from '#/page/frontends/index';
import FrontendsListingPageDefault from '#/page/frontends/index';

describe('Frontends index', () => {
  test('should export FrontendsListingPage', () => {
    expect(FrontendsListingPage).toBeDefined();
    expect(typeof FrontendsListingPage).toBe('function');
  });

  test('should export default as FrontendsListingPage', () => {
    expect(FrontendsListingPageDefault).toBeDefined();
    expect(typeof FrontendsListingPageDefault).toBe('function');
    expect(FrontendsListingPageDefault).toBe(FrontendsListingPage);
  });
});
