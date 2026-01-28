import "@testing-library/jest-dom";
import resettableRootReducer, {
  logout,
  handleLogout,
} from "#/lib/store/rootReducer";

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
Object.defineProperty(window, "localStorage", {
  value: localStorageMock,
});

// Mock window.location
const originalLocation = window.location;
beforeAll(() => {
  delete (window as any).location;
  (window as any).location = {
    href: "",
  };
});

afterAll(() => {
  (window as any).location = originalLocation;
});

// Mock console.log
const originalConsoleLog = console.log;
beforeAll(() => {
  console.log = jest.fn();
});

afterAll(() => {
  console.log = originalConsoleLog;
});

describe("Root Reducer", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (window as any).location.href = "";
  });

  describe("logout action", () => {
    test("should create logout action", () => {
      const action = logout();
      expect(action.type).toBe("logout");
      expect(action.payload).toBeUndefined();
    });

    test("should create logout action with payload", () => {
      const payload = "/custom-logout-path";
      const action = logout(payload);
      expect(action.type).toBe("logout");
      expect(action.payload).toBe(payload);
    });
  });

  describe("handleLogout", () => {
    test("should remove admin access key from localStorage", () => {
      handleLogout();
      expect(localStorageMock.removeItem).toHaveBeenCalledWith(
        "adminAccessKey"
      );
    });

    test("should remove server URL from localStorage", () => {
      handleLogout();
      expect(localStorageMock.removeItem).toHaveBeenCalledWith("serverUrl");
    });

    test("should redirect to default login path when no path provided", () => {
      handleLogout();
      expect((window as any).location.href).toBe("/");
    });

    test("should redirect to custom path when provided", () => {
      const customPath = "/custom-logout";
      handleLogout(customPath);
      expect((window as any).location.href).toBe(customPath);
    });

    test("should call console.log", () => {
      handleLogout();
      expect(console.log).toHaveBeenCalledWith("handleLogout");
    });

    test("should handle multiple logout calls", () => {
      handleLogout("/path1");
      handleLogout("/path2");
      handleLogout();

      expect(localStorageMock.removeItem).toHaveBeenCalledTimes(6); // 2 items * 3 calls
      expect(console.log).toHaveBeenCalledTimes(3);
    });

    test("should handle logout with null path", () => {
      handleLogout(null as any);
      expect((window as any).location.href).toBe("/");
    });

    test("should handle logout with undefined path", () => {
      handleLogout(undefined as any);
      expect((window as any).location.href).toBe("/");
    });
  });

  //   describe('resettableRootReducer', () => {
  //     test('should return root reducer when action is not logout', () => {
  //       const initialState = {
  //         ollamaFlow: { adminAccessKey: 'test-key' },
  //         [apiSlice.reducerPath]: { queries: {}, mutations: {}, provided: {}, subscriptions: {} },
  //       };

  //       const action = { type: 'some/other/action' };
  //       const result = resettableRootReducer(initialState, action);

  //       // Should return the same state structure
  //       expect(result).toHaveProperty('ollamaFlow');
  //       expect(result).toHaveProperty(apiSlice.reducerPath);
  //     });

  //     test('should call handleLogout when logout action is dispatched', () => {
  //       const initialState = {
  //         ollamaFlow: { adminAccessKey: 'test-key' },
  //         [apiSlice.reducerPath]: { queries: {}, mutations: {}, provided: {}, subscriptions: {} },
  //       };

  //       const action = logout('/custom-path');
  //       resettableRootReducer(initialState, action);

  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('adminAccessKey');
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('serverUrl');
  //       expect((window as any).location.href).toBe('/custom-path');
  //     });

  //     test('should call handleLogout with default path when logout action has no payload', () => {
  //       const initialState = {
  //         ollamaFlow: { adminAccessKey: 'test-key' },
  //         [apiSlice.reducerPath]: { queries: {}, mutations: {}, provided: {}, subscriptions: {} },
  //       };

  //       const action = logout();
  //       resettableRootReducer(initialState, action);

  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('adminAccessKey');
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('serverUrl');
  //       expect((window as any).location.href).toBe('/login');
  //     });

  //     test('should handle undefined state', () => {
  //       const action = logout();
  //       const result = resettableRootReducer(undefined, action);

  //       expect(result).toBeDefined();
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('adminAccessKey');
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('serverUrl');
  //     });

  //     test('should handle null state', () => {
  //       const action = logout();
  //       const result = resettableRootReducer(null as any, action);

  //       expect(result).toBeDefined();
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('adminAccessKey');
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('serverUrl');
  //     });

  //     test('should handle empty state object', () => {
  //       const initialState = {};
  //       const action = logout();
  //       const result = resettableRootReducer(initialState, action);

  //       expect(result).toBeDefined();
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('adminAccessKey');
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('serverUrl');
  //     });

  //     test('should handle state with only ollamaFlow', () => {
  //       const initialState = {
  //         ollamaFlow: { adminAccessKey: 'test-key' },
  //       };

  //       const action = { type: 'some/other/action' };
  //       const result = resettableRootReducer(initialState, action);

  //       expect(result).toHaveProperty('ollamaFlow');
  //     });

  //     test('should handle state with only apiSlice', () => {
  //       const initialState = {
  //         [apiSlice.reducerPath]: { queries: {}, mutations: {}, provided: {}, subscriptions: {} },
  //       };

  //       const action = { type: 'some/other/action' };
  //       const result = resettableRootReducer(initialState, action);

  //       expect(result).toHaveProperty(apiSlice.reducerPath);
  //     });

  //     test('should handle logout action with complex payload', () => {
  //       const initialState = {
  //         ollamaFlow: { adminAccessKey: 'test-key' },
  //         [apiSlice.reducerPath]: { queries: {}, mutations: {}, provided: {}, subscriptions: {} },
  //       };

  //       const complexPayload = {
  //         path: '/custom-path',
  //         reason: 'session-expired',
  //         timestamp: Date.now(),
  //       };

  //       const action = logout(complexPayload);
  //       resettableRootReducer(initialState, action);

  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('adminAccessKey');
  //       expect(localStorageMock.removeItem).toHaveBeenCalledWith('serverUrl');
  //       expect((window as any).location.href).toBe(complexPayload);
  //     });

  //     test('should handle multiple logout actions in sequence', () => {
  //       const initialState = {
  //         ollamaFlow: { adminAccessKey: 'test-key' },
  //         [apiSlice.reducerPath]: { queries: {}, mutations: {}, provided: {}, subscriptions: {} },
  //       };

  //       const action1 = logout('/path1');
  //       const action2 = logout('/path2');
  //       const action3 = logout();

  //       resettableRootReducer(initialState, action1);
  //       resettableRootReducer(initialState, action2);
  //       resettableRootReducer(initialState, action3);

  //       expect(localStorageMock.removeItem).toHaveBeenCalledTimes(6); // 2 items * 3 calls
  //       expect(console.log).toHaveBeenCalledTimes(3);
  //     });
  //   });
});
