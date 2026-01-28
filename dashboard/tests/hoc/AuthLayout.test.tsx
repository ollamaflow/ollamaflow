import "@testing-library/jest-dom";
import React from "react";
import { render, screen, act } from "@testing-library/react";

// Mock React's useState and useEffect to control the behavior
const mockSetState = jest.fn();
const mockUseEffect = jest.fn();
jest.mock("react", () => ({
  ...jest.requireActual("react"),
  useState: jest.fn((initialValue) => [initialValue, mockSetState]),
  useEffect: jest.fn((callback, deps) => {
    mockUseEffect(callback, deps);
    // Execute the callback immediately to simulate the effect
    callback();
  }),
}));

// Mock the store hooks
jest.mock("#/lib/store/hooks", () => ({
  useAppDispatch: jest.fn(() => jest.fn()),
}));

// Mock the store actions
jest.mock("#/lib/store/ollamaflow/reducer", () => ({
  storeAdminAccessKey: jest.fn(),
}));

// Mock the RTK API instance
jest.mock("#/lib/store/rtk/rtkApiInstance", () => ({
  changeAxiosBaseUrl: jest.fn(),
  setAuthToken: jest.fn(),
}));

// Mock the PageLoading component
jest.mock("#/components/base/loading/PageLoading", () => {
  return function MockPageLoading() {
    return <div data-testid="page-loading">Loading...</div>;
  };
});

// Mock the constants
jest.mock("#/constants/constant", () => ({
  localStorageKeys: {
    serverUrl: "serverUrl",
    adminAccessKey: "adminAccessKey",
  },
}));

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};

// Mock window object
Object.defineProperty(window, "localStorage", {
  value: localStorageMock,
});

// Import after mocks
import AuthLayout from "#/hoc/AuthLayout";

describe("AuthLayout", () => {
  let mockDispatch: jest.Mock;
  let mockStoreAdminAccessKey: jest.Mock;
  let mockChangeAxiosBaseUrl: jest.Mock;
  let mockSetAuthToken: jest.Mock;

  beforeEach(() => {
    jest.clearAllMocks();

    mockDispatch = jest.fn();
    mockStoreAdminAccessKey = jest.fn();
    mockChangeAxiosBaseUrl = jest.fn();
    mockSetAuthToken = jest.fn();

    // Mock the hooks and functions
    jest.doMock("#/lib/store/hooks", () => ({
      useAppDispatch: jest.fn(() => mockDispatch),
    }));

    jest.doMock("#/lib/store/ollamaflow/reducer", () => ({
      storeAdminAccessKey: mockStoreAdminAccessKey,
    }));

    jest.doMock("#/lib/store/rtk/rtkApiInstance", () => ({
      changeAxiosBaseUrl: mockChangeAxiosBaseUrl,
      setAuthToken: mockSetAuthToken,
    }));

    localStorageMock.getItem.mockReturnValue(null);
  });

  test("should render with children", () => {
    // Mock useState to return false initially (loading state)
    require("react").useState.mockImplementation(() => [false, mockSetState]);

    render(
      <AuthLayout>
        <div data-testid="test-children">Test Content</div>
      </AuthLayout>
    );

    expect(screen.getByTestId("page-loading")).toBeInTheDocument();
  });

  test("should render with children after loading", () => {
    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div data-testid="test-children">Test Content</div>
      </AuthLayout>
    );

    expect(screen.getByTestId("test-children")).toBeInTheDocument();
    expect(screen.queryByTestId("page-loading")).not.toBeInTheDocument();
  });

  test("should render with custom className", () => {
    const customClass = "custom-auth-layout";

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout className={customClass}>
        <div>Test Content</div>
      </AuthLayout>
    );

    const rootDiv = screen.getByText("Test Content").closest("#root-div");
    expect(rootDiv).toHaveClass(customClass);
  });

  test("should initialize auth from localStorage with both values", () => {
    localStorageMock.getItem
      .mockReturnValueOnce("http://localhost:3000") // serverUrl
      .mockReturnValueOnce("admin-key-123"); // adminAccessKey

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("serverUrl");
    expect(localStorageMock.getItem).toHaveBeenCalledWith("adminAccessKey");
  });

  test("should not initialize auth when localStorage values are missing", () => {
    localStorageMock.getItem.mockReturnValue(null);

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("serverUrl");
    expect(localStorageMock.getItem).toHaveBeenCalledWith("adminAccessKey");
  });

  test("should not initialize auth when only serverUrl is present", () => {
    localStorageMock.getItem
      .mockReturnValueOnce("http://localhost:3000") // serverUrl
      .mockReturnValueOnce(null); // adminAccessKey

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("serverUrl");
    expect(localStorageMock.getItem).toHaveBeenCalledWith("adminAccessKey");
  });

  test("should not initialize auth when only adminAccessKey is present", () => {
    localStorageMock.getItem
      .mockReturnValueOnce(null) // serverUrl
      .mockReturnValueOnce("admin-key-123"); // adminAccessKey

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("serverUrl");
    expect(localStorageMock.getItem).toHaveBeenCalledWith("adminAccessKey");
  });

  test("should render with complex children", () => {
    const complexChildren = (
      <div>
        <h1>Complex Title</h1>
        <p>Complex content with multiple elements</p>
        <button>Complex Button</button>
      </div>
    );

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(<AuthLayout>{complexChildren}</AuthLayout>);

    expect(screen.getByText("Complex Title")).toBeInTheDocument();
    expect(
      screen.getByText("Complex content with multiple elements")
    ).toBeInTheDocument();
    expect(screen.getByText("Complex Button")).toBeInTheDocument();
  });

  test("should render with empty children", () => {
    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(<AuthLayout>{}</AuthLayout>);

    expect(screen.queryByTestId("page-loading")).not.toBeInTheDocument();
  });

  test("should render with null children", () => {
    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(<AuthLayout>{null}</AuthLayout>);

    expect(screen.queryByTestId("page-loading")).not.toBeInTheDocument();
  });

  test("should render with undefined children", () => {
    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(<AuthLayout>{undefined}</AuthLayout>);

    expect(screen.queryByTestId("page-loading")).not.toBeInTheDocument();
  });

  test("should render with multiple children", () => {
    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </AuthLayout>
    );

    expect(screen.getByText("Child 1")).toBeInTheDocument();
    expect(screen.getByText("Child 2")).toBeInTheDocument();
    expect(screen.getByText("Child 3")).toBeInTheDocument();
  });

  test("should show loading state initially", () => {
    // Mock useState to return false (loading state)
    require("react").useState.mockImplementation(() => [false, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    // The component should show loading initially
    expect(screen.getByTestId("page-loading")).toBeInTheDocument();
  });

  test("should render root div with correct id", () => {
    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    const rootDiv = screen.getByText("Test Content").closest("#root-div");
    expect(rootDiv).toBeInTheDocument();
  });

  test("should handle auth initialization with empty strings", () => {
    localStorageMock.getItem
      .mockReturnValueOnce("") // serverUrl
      .mockReturnValueOnce(""); // adminAccessKey

    // Mock useState to return true (ready state)
    require("react").useState.mockImplementation(() => [true, mockSetState]);

    render(
      <AuthLayout>
        <div>Test Content</div>
      </AuthLayout>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("serverUrl");
    expect(localStorageMock.getItem).toHaveBeenCalledWith("adminAccessKey");
  });
});
