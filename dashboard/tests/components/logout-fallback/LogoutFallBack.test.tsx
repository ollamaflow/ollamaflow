import "@testing-library/jest-dom";
import React from "react";
import { render, screen, act } from "@testing-library/react";
import LogoutFallBack from "#/components/logout-fallback/LogoutFallBack";

// Mock the useLogout hook
const mockLogout = jest.fn();
jest.mock("#/hooks/authHooks", () => ({
  useLogout: () => mockLogout,
}));

// Mock the FallBack component
jest.mock("#/components/fallback/FallBack", () => {
  return function MockFallBack({
    children,
    className,
  }: {
    children: React.ReactNode;
    className?: string;
  }) {
    return (
      <div data-testid="fallback" className={className}>
        {children}
      </div>
    );
  };
});

describe("LogoutFallBack", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  test("should render with default message and countdown", () => {
    render(<LogoutFallBack />);

    expect(screen.getByTestId("fallback")).toBeInTheDocument();
    expect(screen.getByText(/Session invalid./)).toBeInTheDocument();
    expect(screen.getByText(/Logging out in 3 seconds/)).toBeInTheDocument();
  });

  test("should render with custom message", () => {
    const customMessage = "Custom logout message";
    render(<LogoutFallBack message={customMessage} />);

    const elements = screen.getAllByText((content, element) => {
      return element?.textContent?.includes(customMessage) || false;
    });
    expect(elements.length).toBeGreaterThan(0);
    expect(screen.getByText(/Logging out in 3 seconds/)).toBeInTheDocument();
  });

  test("should countdown from 3 to 0", async () => {
    render(<LogoutFallBack />);

    // Initial countdown should be 3
    expect(screen.getByText(/Logging out in 3 seconds/)).toBeInTheDocument();

    // Fast-forward 1 second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 2 seconds/)).toBeInTheDocument();

    // Fast-forward another second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 1 seconds/)).toBeInTheDocument();

    // Fast-forward final second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 0 seconds/)).toBeInTheDocument();
  });

  test("should call logout when countdown reaches 0", async () => {
    render(<LogoutFallBack />);

    // Check initial state
    expect(screen.getByText(/Logging out in 3 seconds/)).toBeInTheDocument();

    // Fast-forward 1 second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 2 seconds/)).toBeInTheDocument();

    // Fast-forward another second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 1 seconds/)).toBeInTheDocument();

    // Fast-forward final second
    act(() => {
      jest.advanceTimersByTime(1000);
    });
    expect(screen.getByText(/Logging out in 0 seconds/)).toBeInTheDocument();

    // Need to wait for the next tick to ensure the effect runs
    act(() => {
      jest.advanceTimersByTime(1000);
    });

    // The logout should be called when countdown reaches 0
    expect(mockLogout).toHaveBeenCalledTimes(1);
  });

  test("should have correct className on fallback component", () => {
    render(<LogoutFallBack />);

    const fallback = screen.getByTestId("fallback");
    expect(fallback).toHaveClass("mt-12", "pt-12");
  });

  test("should cleanup timer on unmount", () => {
    const { unmount } = render(<LogoutFallBack />);

    unmount();

    // Timer should be cleared, so no more countdown updates
    act(() => {
      jest.advanceTimersByTime(1000);
    });

    // Since component is unmounted, logout should not be called
    expect(mockLogout).not.toHaveBeenCalled();
  });

  test("should render with custom message and countdown", () => {
    const customMessage = "Please wait while we log you out";
    render(<LogoutFallBack message={customMessage} />);

    const elements = screen.getAllByText((content, element) => {
      return element?.textContent?.includes(customMessage) || false;
    });
    expect(elements.length).toBeGreaterThan(0);
    expect(screen.getByText(/Logging out in 3 seconds/)).toBeInTheDocument();
  });
});
