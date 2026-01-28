import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import ErrorBoundary from "#/hoc/ErrorBoundary";

// Mock the FallBack component
jest.mock("#/components/base/fallback/FallBack", () => {
  return function MockFallBack({
    children,
    retry,
    allowRefresh,
  }: {
    children: React.ReactNode;
    retry?: () => void;
    allowRefresh?: boolean;
  }) {
    return (
      <div data-testid="fallback">
        {children}
        {retry && <button onClick={retry}>Retry</button>}
        {allowRefresh && <div>Reload page</div>}
      </div>
    );
  };
});

// Mock console.error to avoid noise in tests
const originalConsoleError = console.error;
beforeAll(() => {
  console.error = jest.fn();
});

afterAll(() => {
  console.error = originalConsoleError;
});

// Component that throws an error
const ThrowError = ({ shouldThrow }: { shouldThrow: boolean }) => {
  if (shouldThrow) {
    throw new Error("Test error message");
  }
  return <div>Normal content</div>;
};

describe("ErrorBoundary", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("should render children when no error occurs", () => {
    render(
      <ErrorBoundary>
        <div data-testid="normal-content">Normal content</div>
      </ErrorBoundary>
    );

    expect(screen.getByTestId("normal-content")).toBeInTheDocument();
    expect(screen.queryByTestId("fallback")).not.toBeInTheDocument();
  });

  test("should render fallback when error occurs", () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(
      screen.getByText("Unexpected Error: Test error message.")
    ).toBeInTheDocument();
  });

  test("should render custom error component when provided", () => {
    const customErrorComponent = (errorMessage?: string) => (
      <div data-testid="custom-error">Custom error: {errorMessage}</div>
    );

    render(
      <ErrorBoundary errorComponent={customErrorComponent}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("custom-error")).toBeInTheDocument();
    expect(
      screen.getByText("Custom error: Test error message")
    ).toBeInTheDocument();
  });

  test("should render reload link when allowRefresh is true", () => {
    render(
      <ErrorBoundary allowRefresh={true}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(screen.getByText("Reload page")).toBeInTheDocument();
  });

  test("should not render reload link when allowRefresh is false", () => {
    render(
      <ErrorBoundary allowRefresh={false}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(screen.queryByText("Reload page")).not.toBeInTheDocument();
  });

  test("should not render reload link when allowRefresh is undefined", () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(screen.queryByText("Reload page")).not.toBeInTheDocument();
  });

  test("should handle error with empty message", () => {
    const ThrowEmptyError = () => {
      throw new Error("");
    };

    render(
      <ErrorBoundary>
        <ThrowEmptyError />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(screen.getByText("Unexpected Error: .")).toBeInTheDocument();
  });

  test("should handle error with undefined message", () => {
    const ThrowUndefinedError = () => {
      const error = new Error();
      error.message = undefined as any;
      throw error;
    };

    render(
      <ErrorBoundary>
        <ThrowUndefinedError />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(screen.getByText(/Unexpected Error:/)).toBeInTheDocument();
  });

  test("should handle custom error component with undefined error message", () => {
    const customErrorComponent = (errorMessage?: string) => (
      <div data-testid="custom-error">
        Custom error: {errorMessage || "No message"}
      </div>
    );

    const ThrowUndefinedError = () => {
      const error = new Error();
      error.message = undefined as any;
      throw error;
    };

    render(
      <ErrorBoundary errorComponent={customErrorComponent}>
        <ThrowUndefinedError />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("custom-error")).toBeInTheDocument();
    expect(screen.getByText(/Custom error:/)).toBeInTheDocument();
  });

  test("should render with complex children", () => {
    const complexChildren = (
      <div>
        <h1>Complex Title</h1>
        <p>Complex content with multiple elements</p>
        <button>Complex Button</button>
      </div>
    );

    render(<ErrorBoundary>{complexChildren}</ErrorBoundary>);

    expect(screen.getByText("Complex Title")).toBeInTheDocument();
    expect(
      screen.getByText("Complex content with multiple elements")
    ).toBeInTheDocument();
    expect(screen.getByText("Complex Button")).toBeInTheDocument();
  });

  test("should render with empty children", () => {
    render(<ErrorBoundary>{}</ErrorBoundary>);

    expect(screen.queryByTestId("fallback")).not.toBeInTheDocument();
  });

  test("should render with null children", () => {
    render(<ErrorBoundary>{null}</ErrorBoundary>);

    expect(screen.queryByTestId("fallback")).not.toBeInTheDocument();
  });

  test("should render with undefined children", () => {
    render(<ErrorBoundary>{undefined}</ErrorBoundary>);

    expect(screen.queryByTestId("fallback")).not.toBeInTheDocument();
  });

  test("should render with multiple children", () => {
    render(
      <ErrorBoundary>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </ErrorBoundary>
    );

    expect(screen.getByText("Child 1")).toBeInTheDocument();
    expect(screen.getByText("Child 2")).toBeInTheDocument();
    expect(screen.getByText("Child 3")).toBeInTheDocument();
  });

  test("should handle error in nested component", () => {
    const NestedComponent = () => (
      <div>
        <div>Outer content</div>
        <ThrowError shouldThrow={true} />
      </div>
    );

    render(
      <ErrorBoundary>
        <NestedComponent />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(
      screen.getByText("Unexpected Error: Test error message.")
    ).toBeInTheDocument();
  });

  test("should handle error with all props combined", () => {
    const customErrorComponent = (errorMessage?: string) => (
      <div data-testid="custom-error">
        Custom error: {errorMessage}
        <div>Reload page</div>
      </div>
    );

    render(
      <ErrorBoundary errorComponent={customErrorComponent} allowRefresh={true}>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("custom-error")).toBeInTheDocument();
    expect(
      screen.getByText(/Custom error: Test error message/)
    ).toBeInTheDocument();
    expect(screen.getByText("Reload page")).toBeInTheDocument();
  });

  test("should call console.error when error occurs", () => {
    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(console.error).toHaveBeenCalled();
  });

  test("should handle error without console.error being called", () => {
    // Temporarily disable console.error
    const originalConsoleError = console.error;
    console.error = jest.fn();

    render(
      <ErrorBoundary>
        <ThrowError shouldThrow={true} />
      </ErrorBoundary>
    );

    expect(screen.getByTestId("fallback")).toBeInTheDocument();

    // Restore console.error
    console.error = originalConsoleError;
  });
});
