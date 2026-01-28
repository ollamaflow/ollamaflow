/* eslint-disable @typescript-eslint/no-require-imports */
import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { ollamaServerUrl } from '#/constants/apiConfig';
import { mockFrontendData, mockFrontendIdentifier } from '../../../../mockData';
import { renderWithRedux } from '../../../../store/utils';
import { createMockInitialState } from '../../../../store/mockStore';
import EditFrontendPage from '#/app/dashboard/frontends/edit/[id]/page';

// Mock next/navigation
const mockPush = jest.fn();
jest.mock('next/navigation', () => ({
  useParams: () => ({ id: mockFrontendIdentifier }),
  useRouter: () => ({
    push: mockPush,
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
}));

// Mock the CreateEditFrontend component
jest.mock('#/page/create-frontend/CreateEditFrontend', () => {
  return function MockCreateEditFrontend({ mode, onSubmit, loading }: any) {
    return (
      <div data-testid="create-edit-frontend">
        <div data-testid="mode">{mode}</div>
        <div data-testid="loading">{loading ? 'loading' : 'not-loading'}</div>
        <button onClick={() => onSubmit({ Name: 'Test Frontend', Hostname: 'localhost' })}>Submit</button>
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
  http.get(`${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`, () => {
    return HttpResponse.json(mockFrontendData);
  }),
  http.put(`${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`, () => {
    return HttpResponse.json(mockFrontendData);
  })
);

describe('EditFrontendPage', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
    mockPush.mockClear();
  });
  afterAll(() => server.close());

  test('should render loading state initially', () => {
    server.use(
      http.get(`${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`, () => {
        return new Promise(() => {}); // Never resolves to simulate loading
      })
    );

    renderWithRedux(<EditFrontendPage />, createMockInitialState());

    expect(screen.getByText('Loading frontend data...')).toBeInTheDocument();
  });

  test('should render error state when frontend not found', async () => {
    server.use(
      http.get(`${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`, () => {
        return HttpResponse.error();
      })
    );

    renderWithRedux(<EditFrontendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByText('Frontend not found')).toBeInTheDocument();
    });
  });

  test('should render edit form when frontend data is loaded', async () => {
    renderWithRedux(<EditFrontendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-frontend')).toBeInTheDocument();
    });

    expect(screen.getByText('Edit Frontend: Frontend 1')).toBeInTheDocument();
  });

  test('should handle form submission successfully', async () => {
    renderWithRedux(<EditFrontendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-frontend')).toBeInTheDocument();
    });

    const submitButton = screen.getByText('Submit');
    fireEvent.click(submitButton);

    await waitFor(() => {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const { message } = require('antd');
      expect(message.success).toHaveBeenCalledWith('Frontend updated successfully');
    });
  });

  test('should handle form submission error', async () => {
    server.use(
      http.put(`${ollamaServerUrl}/v1.0/frontends/${mockFrontendIdentifier}`, () => {
        return HttpResponse.error();
      })
    );

    renderWithRedux(<EditFrontendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-frontend')).toBeInTheDocument();
    });

    const submitButton = screen.getByText('Submit');
    fireEvent.click(submitButton);

    await waitFor(() => {
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const { message } = require('antd');
      expect(message.error).toHaveBeenCalledWith('Failed to update frontend');
    });
  });

  test('should handle back navigation', async () => {
    renderWithRedux(<EditFrontendPage />, createMockInitialState());

    await waitFor(() => {
      expect(screen.getByTestId('create-edit-frontend')).toBeInTheDocument();
    });

    // Look for the back button with the arrow-left icon
    const backButton = screen.getByRole('button', { name: /arrow-left/i });
    fireEvent.click(backButton);

    expect(mockPush).toHaveBeenCalledWith('/dashboard/frontends');
  });
});
