import '@testing-library/jest-dom';
import Sidebar from '#/components/base/sidebar/index';

describe('Sidebar index', () => {
  test('should export Sidebar component as default', () => {
    expect(Sidebar).toBeDefined();
    expect(typeof Sidebar).toBe('function');
  });
});
