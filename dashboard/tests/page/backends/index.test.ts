import '@testing-library/jest-dom';
import { BackendsListingPage } from '#/page/backends/index';

describe('Backends index', () => {
  test('should export BackendsListingPage', () => {
    expect(BackendsListingPage).toBeDefined();
    expect(typeof BackendsListingPage).toBe('function');
  });
});
