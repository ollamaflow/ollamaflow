import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import ThemeModeSwitch from "#/components/theme-mode-switch/ThemeModeSwitch";
import { ThemeEnum } from "#/types/types";

// Mock the useAppContext hook
const mockSetTheme = jest.fn();
const mockUseAppContext = jest.fn();

jest.mock("#/hooks/appHooks", () => ({
  useAppContext: () => mockUseAppContext(),
}));

// Mock the react-toggle-dark-mode component
jest.mock("react-toggle-dark-mode", () => ({
  DarkModeSwitch: ({
    checked,
    onChange,
    size,
  }: {
    checked: boolean;
    onChange: (checked: boolean) => void;
    size: number;
  }) => {
    return (
      <button
        data-testid="dark-mode-toggle"
        onClick={() => onChange(!checked)}
        style={{ width: size, height: size }}
      >
        {checked ? "🌙" : "☀️"}
      </button>
    );
  },
}));

describe("ThemeModeSwitch", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("should render dark mode toggle when theme is dark", () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.DARK,
      setTheme: mockSetTheme,
    });

    render(<ThemeModeSwitch />);

    const toggle = screen.getByTestId("dark-mode-toggle");
    expect(toggle).toBeInTheDocument();
    expect(toggle).toHaveTextContent("🌙");
  });

  test("should render light mode toggle when theme is light", () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.LIGHT,
      setTheme: mockSetTheme,
    });

    render(<ThemeModeSwitch />);

    const toggle = screen.getByTestId("dark-mode-toggle");
    expect(toggle).toBeInTheDocument();
    expect(toggle).toHaveTextContent("☀️");
  });

  test("should call setTheme with DARK when toggling from light to dark", () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.LIGHT,
      setTheme: mockSetTheme,
    });

    render(<ThemeModeSwitch />);

    const toggle = screen.getByTestId("dark-mode-toggle");
    fireEvent.click(toggle);

    expect(mockSetTheme).toHaveBeenCalledWith(ThemeEnum.DARK);
  });

  test("should call setTheme with LIGHT when toggling from dark to light", () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.DARK,
      setTheme: mockSetTheme,
    });

    render(<ThemeModeSwitch />);

    const toggle = screen.getByTestId("dark-mode-toggle");
    fireEvent.click(toggle);

    expect(mockSetTheme).toHaveBeenCalledWith(ThemeEnum.LIGHT);
  });

  test("should have correct size styling", () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.LIGHT,
      setTheme: mockSetTheme,
    });

    render(<ThemeModeSwitch />);

    const toggle = screen.getByTestId("dark-mode-toggle");
    expect(toggle).toHaveStyle({ width: 20, height: 20 });
  });

  test("should render with correct initial state based on theme", () => {
    // Test with dark theme
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.DARK,
      setTheme: mockSetTheme,
    });

    const { rerender } = render(<ThemeModeSwitch />);
    expect(screen.getByTestId("dark-mode-toggle")).toHaveTextContent("🌙");

    // Test with light theme
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.LIGHT,
      setTheme: mockSetTheme,
    });

    rerender(<ThemeModeSwitch />);
    expect(screen.getByTestId("dark-mode-toggle")).toHaveTextContent("☀️");
  });
});
