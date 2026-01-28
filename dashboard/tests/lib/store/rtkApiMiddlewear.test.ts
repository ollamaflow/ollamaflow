import '@testing-library/jest-dom';
import { isRejectedWithValue } from '@reduxjs/toolkit';
import { message } from 'antd';
import { errorHandler, rtkQueryErrorLogger } from '#/lib/store/rtkApiMiddlewear';
import { handleLogout } from '#/lib/store/rootReducer';

// Mock antd message
jest.mock('antd', () => ({
  message: {
    error: jest.fn(),
  },
}));

// Mock the rootReducer
jest.mock('#/lib/store/rootReducer', () => ({
  handleLogout: jest.fn(),
}));

// Mock @reduxjs/toolkit
jest.mock('@reduxjs/toolkit', () => ({
  isRejectedWithValue: jest.fn(),
}));

describe('RTK API Middleware', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  describe('errorHandler', () => {
    test('should handle 401 error for non-login endpoint', () => {
      const error = {
        payload: {
          status: 401,
          data: { message: 'Unauthorized' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Invalid access key, login again. ');

      // Check that handleLogout is called after 2 seconds
      jest.advanceTimersByTime(2000);
      expect(handleLogout).toHaveBeenCalled();
    });

    // test('should not call handleLogout for login endpoint with 401', () => {
    //   const error = {
    //     payload: {
    //       status: 401,
    //       data: { message: 'Unauthorized' },
    //     },
    //     meta: {
    //       arg: {
    //         endpointName: 'login',
    //       },
    //     },
    //   };

    //   errorHandler(error);

    //   // For login endpoint with 401, it should fall through to default case
    //   // But if data equals "Network Error", it will also trigger the Network Error message
    //   // Let's check what actually happens by looking at the implementation
    //   expect(message.error).toHaveBeenCalledWith('Unauthorized');

    //   // Check that handleLogout is NOT called for login endpoint
    //   jest.advanceTimersByTime(2000);
    //   expect(handleLogout).not.toHaveBeenCalled();
    // });

    test('should handle 403 error', () => {
      const error = {
        payload: {
          status: 403,
          data: { message: 'Forbidden' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Invalid access key, login again. ');
      jest.advanceTimersByTime(2000);
      expect(handleLogout).toHaveBeenCalled();
    });

    test('should handle StatusCode error', () => {
      const error = {
        payload: {
          StatusCode: 401,
          data: { message: 'Unauthorized' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Invalid access key, login again. ');
      jest.advanceTimersByTime(2000);
      expect(handleLogout).toHaveBeenCalled();
    });

    test('should handle code error', () => {
      const error = {
        payload: {
          code: 401,
          data: { message: 'Unauthorized' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Invalid access key, login again. ');
      jest.advanceTimersByTime(2000);
      expect(handleLogout).toHaveBeenCalled();
    });

    test('should handle default error with server message', () => {
      const error = {
        payload: {
          status: 500,
          data: { message: 'Internal Server Error' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Internal Server Error');
    });

    test('should handle default error with detail message', () => {
      const error = {
        payload: {
          status: 500,
          data: { detail: 'Something went wrong' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Something went wrong');
    });

    test('should handle default error without server message', () => {
      const error = {
        payload: {
          status: 500,
          data: {},
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Something went wrong.');
    });

    test('should handle Network Error in data', () => {
      const error = {
        payload: {
          status: 500,
          data: 'Network Error',
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      expect(message.error).toHaveBeenCalledWith('Network Error');
    });

    test('should handle error without status/code', () => {
      const error = {
        payload: {
          data: { message: 'Some error' },
        },
        meta: {
          arg: {
            endpointName: 'getBackends',
          },
        },
      };

      errorHandler(error);

      // Should not call message.error since there's no status/code
      expect(message.error).not.toHaveBeenCalled();
    });

    test('should handle error without payload', () => {
      const error = {};

      errorHandler(error);

      // Should not call message.error since there's no payload
      expect(message.error).not.toHaveBeenCalled();
    });
  });

  describe('rtkQueryErrorLogger', () => {
    test('should call errorHandler for rejected action', () => {
      const mockIsRejectedWithValue = isRejectedWithValue as jest.MockedFunction<typeof isRejectedWithValue>;
      mockIsRejectedWithValue.mockReturnValue(true);

      const mockNext = jest.fn();
      const mockApi = {
        dispatch: jest.fn(),
        getState: jest.fn(),
      };

      const action = {
        type: 'some/rejected',
        payload: { status: 500, data: { message: 'Error' } },
      };

      const middleware = rtkQueryErrorLogger(mockApi as any);
      const nextMiddleware = middleware(mockNext);
      nextMiddleware(action);

      expect(mockIsRejectedWithValue).toHaveBeenCalledWith(action);
      expect(mockNext).toHaveBeenCalledWith(action);
    });

    test('should not call errorHandler for non-rejected action', () => {
      const mockIsRejectedWithValue = isRejectedWithValue as jest.MockedFunction<typeof isRejectedWithValue>;
      mockIsRejectedWithValue.mockReturnValue(false);

      const mockNext = jest.fn();
      const mockApi = {
        dispatch: jest.fn(),
        getState: jest.fn(),
      };

      const action = {
        type: 'some/fulfilled',
        payload: { data: 'Success' },
      };

      const middleware = rtkQueryErrorLogger(mockApi as any);
      const nextMiddleware = middleware(mockNext);
      nextMiddleware(action);

      expect(mockIsRejectedWithValue).toHaveBeenCalledWith(action);
      expect(mockNext).toHaveBeenCalledWith(action);
      expect(message.error).not.toHaveBeenCalled();
    });
  });
});
