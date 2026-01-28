/* eslint-disable @typescript-eslint/no-require-imports */
import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import StoreProvider from '#/lib/store/StoreProvider';

// Mock the store
jest.mock('#/lib/store/store', () => ({
  makeStore: jest.fn(() => ({
    getState: jest.fn(),
    dispatch: jest.fn(),
    subscribe: jest.fn(),
  })),
}));

describe('StoreProvider', () => {
  test('should render children wrapped in Provider', () => {
    const TestComponent = () => <div data-testid="test-child">Test Child</div>;

    const { getByTestId } = render(
      <StoreProvider>
        <TestComponent />
      </StoreProvider>
    );

    expect(getByTestId('test-child')).toBeInTheDocument();
    expect(getByTestId('test-child')).toHaveTextContent('Test Child');
  });

  test('should create store instance', () => {
    const { makeStore } = require('#/lib/store/store');

    render(
      <StoreProvider>
        <div>Test</div>
      </StoreProvider>
    );

    expect(makeStore).toHaveBeenCalled();
  });

  test('should render multiple children', () => {
    const { getByTestId } = render(
      <StoreProvider>
        <div data-testid="child1">Child 1</div>
        <div data-testid="child2">Child 2</div>
      </StoreProvider>
    );

    expect(getByTestId('child1')).toBeInTheDocument();
    expect(getByTestId('child2')).toBeInTheDocument();
  });
});
