import '@testing-library/jest-dom';
import { CreateComponentPage, CreateEditFrontend } from '#/page/create-frontend/index';
import CreateComponentPageDefault from '#/page/create-frontend/index';

describe('Create Frontend index', () => {
  test('should export CreateComponentPage', () => {
    expect(CreateComponentPage).toBeDefined();
    expect(typeof CreateComponentPage).toBe('function');
  });

  test('should export CreateEditFrontend', () => {
    expect(CreateEditFrontend).toBeDefined();
    expect(typeof CreateEditFrontend).toBe('function');
  });

  test('should export default as CreateComponentPage', () => {
    expect(CreateComponentPageDefault).toBeDefined();
    expect(typeof CreateComponentPageDefault).toBe('function');
    expect(CreateComponentPageDefault).toBe(CreateComponentPage);
  });
});
