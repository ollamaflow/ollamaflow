import '@testing-library/jest-dom';
import { makeStore } from '#/lib/store/store';

// Mock the rootReducer
jest.mock('#/lib/store/rootReducer', () => ({
  __esModule: true,
  default: jest.fn((state = {}) => state),
  apiMiddleWares: [],
}));

describe('Store', () => {
  test('should create store with makeStore', () => {
    const store = makeStore();

    expect(store).toBeDefined();
    expect(typeof store.getState).toBe('function');
    expect(typeof store.dispatch).toBe('function');
    expect(typeof store.subscribe).toBe('function');
  });

  test('should create store with proper configuration', () => {
    const store = makeStore();
    const state = store.getState();

    expect(state).toBeDefined();
  });

  test('should have correct store structure', () => {
    const store = makeStore();

    // Test that the store has the expected methods
    expect(store).toHaveProperty('getState');
    expect(store).toHaveProperty('dispatch');
    expect(store).toHaveProperty('subscribe');
    expect(store).toHaveProperty('replaceReducer');
  });

  test('should return state from getState', () => {
    const store = makeStore();
    const state = store.getState();

    expect(state).toBeDefined();
    expect(typeof state).toBe('object');
  });
});
