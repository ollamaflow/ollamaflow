import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import DashboardLayout from "#/components/layout/DashboardLayout";
import { ThemeEnum } from "#/types/types";

// Mock the app hooks
jest.mock("#/hooks/appHooks", () => ({
  useAppContext: jest.fn(() => ({ theme: ThemeEnum.LIGHT })),
}));

// Mock the auth hooks
jest.mock("#/hooks/authHooks", () => ({
  useLogout: jest.fn(() => jest.fn()),
}));

// Mock the sidebar component
jest.mock("#/components/base/sidebar", () => {
  return function MockSidebar({ children }: { children: React.ReactNode }) {
    return <div data-testid="sidebar">{children}</div>;
  };
});

// Mock the theme mode switch component
jest.mock("#/components/theme-mode-switch/ThemeModeSwitch", () => {
  return function MockThemeModeSwitch() {
    return <div data-testid="theme-mode-switch">Theme Switch</div>;
  };
});

// Mock the tooltip component
jest.mock("#/components/base/tooltip/Tooltip", () => {
  return function MockTooltip({
    children,
    title,
  }: {
    children: React.ReactNode;
    title: string;
  }) {
    return (
      <div data-testid="tooltip" title={title}>
        {children}
      </div>
    );
  };
});

// Mock the button component
jest.mock("#/components/base/button/Button", () => {
  return function MockButton({
    children,
    onClick,
    icon,
  }: {
    children: React.ReactNode;
    onClick?: () => void;
    icon?: React.ReactNode;
  }) {
    return (
      <button data-testid="logout-button" onClick={onClick}>
        {icon}
        {children}
      </button>
    );
  };
});

describe("DashboardLayout", () => {
  const mockUseAppContext = require("#/hooks/appHooks").useAppContext;
  const mockUseLogout = require("#/hooks/authHooks").useLogout;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("should render dashboard layout with children", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div data-testid="test-children">Test Content</div>
      </DashboardLayout>
    );

    expect(screen.getByTestId("sidebar")).toBeInTheDocument();
    expect(screen.getByTestId("test-children")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("logout-button")).toBeInTheDocument();
  });

  test("should render with light theme", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div>Test Content</div>
      </DashboardLayout>
    );

    const tooltip = screen.getByTestId("tooltip");
    expect(tooltip).toHaveAttribute("title", "Switch to Dark mode");
  });

  test("should render with dark theme", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.DARK });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div>Test Content</div>
      </DashboardLayout>
    );

    const tooltip = screen.getByTestId("tooltip");
    expect(tooltip).toHaveAttribute("title", "Switch to Light mode");
  });

  test("should call logout when logout button is clicked", () => {
    const mockLogout = jest.fn();
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(mockLogout);

    render(
      <DashboardLayout>
        <div>Test Content</div>
      </DashboardLayout>
    );

    const logoutButton = screen.getByTestId("logout-button");
    fireEvent.click(logoutButton);

    expect(mockLogout).toHaveBeenCalledTimes(1);
  });

  test("should render user section with correct test id", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div>Test Content</div>
      </DashboardLayout>
    );

    expect(screen.getByTestId("user-section")).toBeInTheDocument();
  });

  test("should render with complex children", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    const complexChildren = (
      <div>
        <h1>Dashboard Title</h1>
        <p>Dashboard content with multiple elements</p>
        <button>Action Button</button>
      </div>
    );

    render(<DashboardLayout>{complexChildren}</DashboardLayout>);

    expect(screen.getByText("Dashboard Title")).toBeInTheDocument();
    expect(
      screen.getByText("Dashboard content with multiple elements")
    ).toBeInTheDocument();
    expect(screen.getByText("Action Button")).toBeInTheDocument();
  });

  test("should render with empty children", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(<DashboardLayout>{}</DashboardLayout>);

    expect(screen.getByTestId("sidebar")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("logout-button")).toBeInTheDocument();
  });

  test("should render with null children", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(<DashboardLayout>{null}</DashboardLayout>);

    expect(screen.getByTestId("sidebar")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("logout-button")).toBeInTheDocument();
  });

  test("should render with undefined children", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(<DashboardLayout>{undefined}</DashboardLayout>);

    expect(screen.getByTestId("sidebar")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("logout-button")).toBeInTheDocument();
  });

  test("should render with multiple children", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </DashboardLayout>
    );

    expect(screen.getByText("Child 1")).toBeInTheDocument();
    expect(screen.getByText("Child 2")).toBeInTheDocument();
    expect(screen.getByText("Child 3")).toBeInTheDocument();
  });

  test("should render with theme mode switch in tooltip", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div>Test Content</div>
      </DashboardLayout>
    );

    const tooltip = screen.getByTestId("tooltip");
    const themeSwitch = screen.getByTestId("theme-mode-switch");

    expect(tooltip).toContainElement(themeSwitch);
  });

  test("should render logout button with icon", () => {
    mockUseAppContext.mockReturnValue({ theme: ThemeEnum.LIGHT });
    mockUseLogout.mockReturnValue(jest.fn());

    render(
      <DashboardLayout>
        <div>Test Content</div>
      </DashboardLayout>
    );

    const logoutButton = screen.getByTestId("logout-button");
    expect(logoutButton).toBeInTheDocument();
    expect(logoutButton).toHaveTextContent("Logout");
  });
});
