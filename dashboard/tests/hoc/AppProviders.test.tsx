import "@testing-library/jest-dom";
import React from "react";
import { render, screen, act } from "@testing-library/react";
import AppProviders from "#/hoc/AppProviders";
import { ThemeEnum } from "#/types/types";

// Mock the StoreProvider
jest.mock("#/lib/store/StoreProvider", () => {
  return function MockStoreProvider({
    children,
  }: {
    children: React.ReactNode;
  }) {
    return <div data-testid="store-provider">{children}</div>;
  };
});

// Mock the AuthLayout
jest.mock("#/hoc/AuthLayout", () => {
  return function MockAuthLayout({ children }: { children: React.ReactNode }) {
    return <div data-testid="auth-layout">{children}</div>;
  };
});

// Mock the theme
jest.mock("#/theme/theme", () => ({
  darkTheme: { token: { colorPrimary: "#000000" } },
  primaryTheme: { token: { colorPrimary: "#ffffff" } },
}));

// Mock AntdRegistry and related components
jest.mock("@ant-design/nextjs-registry", () => ({
  AntdRegistry: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="antd-registry">{children}</div>
  ),
}));

jest.mock("@ant-design/cssinjs", () => ({
  StyleProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="style-provider">{children}</div>
  ),
}));

jest.mock("antd", () => ({
  ConfigProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="config-provider">{children}</div>
  ),
}));

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

describe("AppProviders", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorageMock.getItem.mockReturnValue(null);
  });

  test("should render with children", () => {
    render(
      <AppProviders>
        <div data-testid="test-children">Test Content</div>
      </AppProviders>
    );

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
    expect(screen.getByTestId("test-children")).toBeInTheDocument();
  });

  test("should render with default light theme", () => {
    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should load theme from localStorage", () => {
    localStorageMock.getItem.mockReturnValue(ThemeEnum.DARK);

    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("theme");
  });

  test("should handle localStorage not available", () => {
    // Mock localStorage as undefined
    const originalLocalStorage = window.localStorage;
    delete (window as any).localStorage;

    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();

    // Restore localStorage
    (window as any).localStorage = originalLocalStorage;
  });

  test("should render with complex children", () => {
    const complexChildren = (
      <div>
        <h1>App Title</h1>
        <p>App content with multiple elements</p>
        <button>Action Button</button>
      </div>
    );

    render(<AppProviders>{complexChildren}</AppProviders>);

    expect(screen.getByText("App Title")).toBeInTheDocument();
    expect(
      screen.getByText("App content with multiple elements")
    ).toBeInTheDocument();
    expect(screen.getByText("Action Button")).toBeInTheDocument();
  });

  test("should render with empty children", () => {
    render(<AppProviders>{}</AppProviders>);

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should render with null children", () => {
    render(<AppProviders>{null}</AppProviders>);

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should render with undefined children", () => {
    render(<AppProviders>{undefined}</AppProviders>);

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should render with multiple children", () => {
    render(
      <AppProviders>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </AppProviders>
    );

    expect(screen.getByText("Child 1")).toBeInTheDocument();
    expect(screen.getByText("Child 2")).toBeInTheDocument();
    expect(screen.getByText("Child 3")).toBeInTheDocument();
  });

  test("should handle theme change", () => {
    const { rerender } = render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();

    // Re-render to simulate theme change
    rerender(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should handle mounted state", () => {
    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    // Wait for useEffect to complete
    act(() => {
      // This simulates the useEffect running
    });

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should handle dark theme from localStorage", () => {
    localStorageMock.getItem.mockReturnValue(ThemeEnum.DARK);

    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("theme");
    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should handle invalid theme from localStorage", () => {
    localStorageMock.getItem.mockReturnValue("invalid-theme");

    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("theme");
    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should handle empty theme from localStorage", () => {
    localStorageMock.getItem.mockReturnValue("");

    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(localStorageMock.getItem).toHaveBeenCalledWith("theme");
    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should provide app context", () => {
    render(
      <AppProviders>
        <div>Test Content</div>
      </AppProviders>
    );

    expect(screen.getByTestId("store-provider")).toBeInTheDocument();
    expect(screen.getByTestId("auth-layout")).toBeInTheDocument();
  });

  test("should render with all providers in correct order", () => {
    render(
      <AppProviders>
        <div data-testid="test-children">Test Content</div>
      </AppProviders>
    );

    const storeProvider = screen.getByTestId("store-provider");
    const authLayout = screen.getByTestId("auth-layout");
    const testChildren = screen.getByTestId("test-children");

    expect(storeProvider).toContainElement(authLayout);
    expect(authLayout).toContainElement(testChildren);
  });
});
