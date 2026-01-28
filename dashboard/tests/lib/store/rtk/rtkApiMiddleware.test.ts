/* eslint-disable @typescript-eslint/no-require-imports */
import '@testing-library/jest-dom';
import { rtkQueryErrorLogger, errorHandler } from '#/lib/store/rtk/rtkApiMiddleware';

// Mock antd message
jest.mock('antd', () => ({
  message: {
    error: jest.fn(),
  },
}));

// Mock @reduxjs/toolkit
jest.mock('@reduxjs/toolkit', () => ({
  isRejectedWithValue: jest.fn(),
}));

// Mock console.log
const consoleSpy = jest.spyOn(console, 'log').mockImplementation(() => {});

describe('RTK API Middleware', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterAll(() => {
    consoleSpy.mockRestore();
  });

  describe('errorHandler', () => {
    test('should handle error with Message property', () => {
      const mockDispatch = jest.fn();
      const error = { payload: { Message: 'Test error message' } };

      errorHandler(error, mockDispatch);

      expect(require('antd').message.error).toHaveBeenCalledWith('Test error message');
    });

    test('should handle error with Description property', () => {
      const mockDispatch = jest.fn();
      const error = { payload: { Description: 'Test error description' } };

      errorHandler(error, mockDispatch);

      expect(require('antd').message.error).toHaveBeenCalledWith('Test error description');
    });

    test('should handle error with message property', () => {
      const mockDispatch = jest.fn();
      const error = { payload: { message: 'Test error message' } };

      errorHandler(error, mockDispatch);

      expect(require('antd').message.error).toHaveBeenCalledWith('Test error message');
    });

    test('should handle network error', () => {
      const mockDispatch = jest.fn();
      const error = { payload: { data: 'Network Error' } };

      errorHandler(error, mockDispatch);

      expect(require('antd').message.error).toHaveBeenCalledWith('Network Error');
    });

    test('should handle generic error', () => {
      const mockDispatch = jest.fn();
      const error = { payload: {} };

      errorHandler(error, mockDispatch);

      expect(require('antd').message.error).toHaveBeenCalledWith('Something went wrong.');
    });

    test('should handle NotAuthorized error', () => {
      const mockDispatch = jest.fn();
      const error = { payload: { Error: 'NotAuthorized' } };

      errorHandler(error, mockDispatch);

      expect(require('antd').message.error).toHaveBeenCalledWith('Session expired. Redirecting to login page...');
    });
  });

  describe('rtkQueryErrorLogger', () => {
    test('should handle rejected action', () => {
      const mockDispatch = jest.fn();
      const mockNext = jest.fn();
      const mockApi = { dispatch: mockDispatch };

      const action = {
        type: 'some/rejected',
        payload: { Message: 'Test error' },
      };

      // Mock isRejectedWithValue to return true
      const { isRejectedWithValue } = require('@reduxjs/toolkit');
      isRejectedWithValue.mockReturnValue(true);

      const middleware = rtkQueryErrorLogger(mockApi as any);
      const nextMiddleware = middleware(mockNext);

      nextMiddleware(action);

      expect(require('antd').message.error).toHaveBeenCalledWith('Test error');
      expect(mockNext).toHaveBeenCalledWith(action);
    });

    test('should pass through non-rejected actions', () => {
      const mockDispatch = jest.fn();
      const mockNext = jest.fn();
      const mockApi = { dispatch: mockDispatch };

      const action = {
        type: 'some/fulfilled',
        payload: { data: 'success' },
      };

      // Mock isRejectedWithValue to return false
      const { isRejectedWithValue } = require('@reduxjs/toolkit');
      isRejectedWithValue.mockReturnValue(false);

      const middleware = rtkQueryErrorLogger(mockApi as any);
      const nextMiddleware = middleware(mockNext);

      nextMiddleware(action);

      expect(require('antd').message.error).not.toHaveBeenCalled();
      expect(mockNext).toHaveBeenCalledWith(action);
    });
  });
});
