import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent, act } from "@testing-library/react";
import { usePagination, useAppContext, AppContext } from "#/hooks/appHooks";
import { ThemeEnum } from "#/types/types";

// Test component for usePagination hook
const TestPaginationComponent = () => {
  const { page, pageSize, skip, handlePageChange } = usePagination();

  return (
    <div>
      <div data-testid="page">Page: {page}</div>
      <div data-testid="page-size">Page Size: {pageSize}</div>
      <div data-testid="skip">Skip: {skip}</div>
      <button onClick={() => handlePageChange(2, 20)}>Change Page</button>
    </div>
  );
};

// Test component for useAppContext hook
const TestAppContextComponent = () => {
  const { theme, setTheme } = useAppContext();

  return (
    <div>
      <div data-testid="theme">Theme: {theme}</div>
      <button onClick={() => setTheme(ThemeEnum.DARK)}>Change Theme</button>
    </div>
  );
};

describe("App Hooks", () => {
  describe("usePagination", () => {
    test("should return default pagination values", () => {
      render(<TestPaginationComponent />);

      expect(screen.getByTestId("page")).toHaveTextContent("Page: 1");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 10"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 0");
    });

    test("should handle page change", () => {
      render(<TestPaginationComponent />);

      const changeButton = screen.getByText("Change Page");
      fireEvent.click(changeButton);

      expect(screen.getByTestId("page")).toHaveTextContent("Page: 2");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 20"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 20");
    });

    test("should calculate skip correctly for different pages", () => {
      render(<TestPaginationComponent />);

      const changeButton = screen.getByText("Change Page");
      fireEvent.click(changeButton);

      // Page 2 with page size 20 should have skip of 20
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 20");
    });

    test("should handle multiple page changes", () => {
      render(<TestPaginationComponent />);

      const changeButton = screen.getByText("Change Page");

      // First change
      fireEvent.click(changeButton);
      expect(screen.getByTestId("page")).toHaveTextContent("Page: 2");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 20"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 20");

      // Second change - click again to simulate another page change
      fireEvent.click(changeButton);
      expect(screen.getByTestId("page")).toHaveTextContent("Page: 2");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 20"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 20");
    });

    test("should handle page change with same page size", () => {
      const TestComponent = () => {
        const { page, pageSize, skip, handlePageChange } = usePagination();

        return (
          <div>
            <div data-testid="page">Page: {page}</div>
            <div data-testid="page-size">Page Size: {pageSize}</div>
            <div data-testid="skip">Skip: {skip}</div>
            <button onClick={() => handlePageChange(3, 10)}>
              Change to Page 3
            </button>
          </div>
        );
      };

      render(<TestComponent />);

      const changeButton = screen.getByText("Change to Page 3");
      fireEvent.click(changeButton);

      expect(screen.getByTestId("page")).toHaveTextContent("Page: 3");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 10"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 20");
    });

    test("should handle page change with different page size", () => {
      const TestComponent = () => {
        const { page, pageSize, skip, handlePageChange } = usePagination();

        return (
          <div>
            <div data-testid="page">Page: {page}</div>
            <div data-testid="page-size">Page Size: {pageSize}</div>
            <div data-testid="skip">Skip: {skip}</div>
            <button onClick={() => handlePageChange(1, 25)}>
              Change Page Size
            </button>
          </div>
        );
      };

      render(<TestComponent />);

      const changeButton = screen.getByText("Change Page Size");
      fireEvent.click(changeButton);

      expect(screen.getByTestId("page")).toHaveTextContent("Page: 1");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 25"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: 0");
    });

    test("should handle edge case with page 0", () => {
      const TestComponent = () => {
        const { page, pageSize, skip, handlePageChange } = usePagination();

        return (
          <div>
            <div data-testid="page">Page: {page}</div>
            <div data-testid="page-size">Page Size: {pageSize}</div>
            <div data-testid="skip">Skip: {skip}</div>
            <button onClick={() => handlePageChange(0, 10)}>
              Change to Page 0
            </button>
          </div>
        );
      };

      render(<TestComponent />);

      const changeButton = screen.getByText("Change to Page 0");
      fireEvent.click(changeButton);

      expect(screen.getByTestId("page")).toHaveTextContent("Page: 0");
      expect(screen.getByTestId("page-size")).toHaveTextContent(
        "Page Size: 10"
      );
      expect(screen.getByTestId("skip")).toHaveTextContent("Skip: -10");
    });
  });

  describe("useAppContext", () => {
    test("should return default context values", () => {
      render(
        <AppContext.Provider
          value={{ theme: ThemeEnum.LIGHT, setTheme: jest.fn() }}
        >
          <TestAppContextComponent />
        </AppContext.Provider>
      );

      expect(screen.getByTestId("theme")).toHaveTextContent("Theme: light");
    });

    test("should return context values when provided", () => {
      const mockSetTheme = jest.fn();

      render(
        <AppContext.Provider
          value={{ theme: ThemeEnum.DARK, setTheme: mockSetTheme }}
        >
          <TestAppContextComponent />
        </AppContext.Provider>
      );

      expect(screen.getByTestId("theme")).toHaveTextContent("Theme: dark");
    });

    test("should call setTheme when button is clicked", () => {
      const mockSetTheme = jest.fn();

      render(
        <AppContext.Provider
          value={{ theme: ThemeEnum.LIGHT, setTheme: mockSetTheme }}
        >
          <TestAppContextComponent />
        </AppContext.Provider>
      );

      const changeButton = screen.getByText("Change Theme");
      fireEvent.click(changeButton);

      expect(mockSetTheme).toHaveBeenCalledWith(ThemeEnum.DARK);
    });

    test("should handle multiple theme changes", () => {
      const mockSetTheme = jest.fn();

      render(
        <AppContext.Provider
          value={{ theme: ThemeEnum.LIGHT, setTheme: mockSetTheme }}
        >
          <TestAppContextComponent />
        </AppContext.Provider>
      );

      const changeButton = screen.getByText("Change Theme");

      // First click
      fireEvent.click(changeButton);
      expect(mockSetTheme).toHaveBeenCalledWith(ThemeEnum.DARK);
      expect(mockSetTheme).toHaveBeenCalledTimes(1);

      // Second click
      fireEvent.click(changeButton);
      expect(mockSetTheme).toHaveBeenCalledWith(ThemeEnum.DARK);
      expect(mockSetTheme).toHaveBeenCalledTimes(2);
    });

    test("should work with default context when no provider", () => {
      render(<TestAppContextComponent />);

      expect(screen.getByTestId("theme")).toHaveTextContent("Theme: light");
    });

    test("should handle context with no-op setTheme", () => {
      render(
        <AppContext.Provider
          value={{ theme: ThemeEnum.LIGHT, setTheme: () => {} }}
        >
          <TestAppContextComponent />
        </AppContext.Provider>
      );

      const changeButton = screen.getByText("Change Theme");
      fireEvent.click(changeButton);

      // Should not throw error
      expect(screen.getByTestId("theme")).toHaveTextContent("Theme: light");
    });

    test("should handle different theme values", () => {
      const themes = [ThemeEnum.LIGHT, ThemeEnum.DARK];

      themes.forEach((theme) => {
        const { unmount } = render(
          <AppContext.Provider value={{ theme, setTheme: jest.fn() }}>
            <TestAppContextComponent />
          </AppContext.Provider>
        );

        expect(screen.getByTestId("theme")).toHaveTextContent(
          `Theme: ${theme}`
        );
        unmount();
      });
    });

    test("should handle complex context values", () => {
      const mockSetTheme = jest.fn();
      const complexContext = {
        theme: ThemeEnum.DARK,
        setTheme: mockSetTheme,
      };

      render(
        <AppContext.Provider value={complexContext}>
          <TestAppContextComponent />
        </AppContext.Provider>
      );

      expect(screen.getByTestId("theme")).toHaveTextContent("Theme: dark");

      const changeButton = screen.getByText("Change Theme");
      fireEvent.click(changeButton);

      expect(mockSetTheme).toHaveBeenCalledWith(ThemeEnum.DARK);
    });
  });
});
