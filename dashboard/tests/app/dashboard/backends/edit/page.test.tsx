/* eslint-disable @typescript-eslint/no-require-imports */
import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { ollamaServerUrl } from '#/constants/apiConfig';
import { mockBackendData, mockBackendIdentifier } from '../../../../mockData';
import { renderWithRedux } from '../../../../store/utils';
import { createMockInitialState } from '../../../../store/mockStore';
import EditBackendPage from '#/app/dashboard/backends/edit/[id]/page';

// Mock next/navigation
const mockPush = jest.fn();
jest.mock('next/navigation', () => ({
  useParams: () => ({ id: mockBackendIdentifier }),
  useRouter: () => ({
    push: mockPush,
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
}));

// Mock the CreateEditBackend component
jest.mock('#/page/create-backend/CreateEditBackend', () => {
  return function MockCreateEditBackend({ mode, onSubmit, loading }: any) {
    return (
      <div data-testid="create-edit-backend">
        <div data-testid="mode">{mode}</div>
        <div data-testid="loading">{loading ? 'loading' : 'not-loading'}</div>
        <button onClick={() => onSubmit({ Name: 'Test Backend', Hostname: 'localhost' })}>Submit</button>
      </div>
    );
  };
});

// Mock antd message
jest.mock('antd', () => ({
  ...jest.requireActual('antd'),
  message: {
    success: jest.fn(),
    error: jest.fn(),
  },
}));

const server = setupServer(
  http.get(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
    return HttpResponse.json(mockBackendData);
  }),
  http.put(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
    return HttpResponse.json(mockBackendData);
  })
);

describe('EditBackendPage', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
    mockPush.mockClear();
  });
  afterAll(() => server.close());

  test('should render loading state initially', () => {
    server.use(
      http.get(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
        return new Promise(() => {}); // Never resolves to simulate loading
      })
    );

    renderWithRedux(<EditBackendPage />, createMockInitialState());

    expect(screen.getByText('Loading backend data...')).toBeInTheDocument();
  });

  test('should render error state when backend not found', async () => {
    server.use(
      http.get(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
        return HttpResponse.error();
      })
    );

    renderWithRedux(<EditBackendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByText('Backend not found')).toBeInTheDocument();
    });
  });

  test('should render edit form when backend data is loaded', async () => {
    renderWithRedux(<EditBackendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-backend')).toBeInTheDocument();
    });

    expect(screen.getByText('Edit Backend: Backend 1')).toBeInTheDocument();
  });

  test('should handle form submission successfully', async () => {
    renderWithRedux(<EditBackendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-backend')).toBeInTheDocument();
    });

    const submitButton = screen.getByText('Submit');
    fireEvent.click(submitButton);

    await waitFor(() => {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const { message } = require('antd');
      expect(message.success).toHaveBeenCalledWith('Backend updated successfully');
    });
  });

  test('should handle form submission error', async () => {
    server.use(
      http.put(`${ollamaServerUrl}/v1.0/backends/${mockBackendIdentifier}`, () => {
        return HttpResponse.error();
      })
    );

    renderWithRedux(<EditBackendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-backend')).toBeInTheDocument();
    });

    const submitButton = screen.getByText('Submit');
    fireEvent.click(submitButton);

    await waitFor(() => {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const { message } = require('antd');
      expect(message.error).toHaveBeenCalledWith('Failed to update backend');
    });
  });

  test('should handle back navigation', async () => {
    renderWithRedux(<EditBackendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-backend')).toBeInTheDocument();
    });

    // Look for the back button with the arrow-left icon
    const backButton = screen.getByRole('button', { name: /arrow-left/i });
    fireEvent.click(backButton);

    expect(mockPush).toHaveBeenCalledWith('/dashboard/backends');
  });
});
