import '@testing-library/jest-dom';
import { CreateComponentPage, CreateEditBackend } from '#/page/create-backend/index';

describe('Create Backend index', () => {
  test('should export CreateComponentPage', () => {
    expect(CreateComponentPage).toBeDefined();
    expect(typeof CreateComponentPage).toBe('function');
  });

  test('should export CreateEditBackend', () => {
    expect(CreateEditBackend).toBeDefined();
    expect(typeof CreateEditBackend).toBe('function');
  });
});
