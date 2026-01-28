import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import Sidebar from "#/components/base/sidebar/Sidebar";

// Mock Next.js navigation hooks
jest.mock("next/navigation", () => ({
  usePathname: jest.fn(),
}));

// Mock the app hooks
jest.mock("#/hooks/appHooks", () => ({
  useAppContext: jest.fn(() => ({})),
}));

// Mock Next.js Link component
jest.mock("next/link", () => {
  return function MockLink({
    children,
    href,
  }: {
    children: React.ReactNode;
    href: string;
  }) {
    return <a href={href}>{children}</a>;
  };
});

describe("Sidebar", () => {
  const mockUsePathname = require("next/navigation").usePathname;
  const mockOnCollapse = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  test("should render sidebar with default collapsed state", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar />);

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByText("Frontends")).toBeInTheDocument();
    expect(screen.getByText("Backends")).toBeInTheDocument();
  });

  test("should render sidebar with collapsed state", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={true} />);

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByText("Frontends")).toBeInTheDocument();
    expect(screen.getByText("Backends")).toBeInTheDocument();
  });

  test("should render sidebar with expanded state", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={false} />);

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByText("Frontends")).toBeInTheDocument();
    expect(screen.getByText("Backends")).toBeInTheDocument();
  });

  test("should call onCollapse when collapse button is clicked", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={false} onCollapse={mockOnCollapse} />);

    const collapseButton = screen.getByTitle("Collapse Sidebar");
    fireEvent.click(collapseButton);

    expect(mockOnCollapse).toHaveBeenCalledWith(true);
  });

  test("should call onCollapse when expand button is clicked", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={true} onCollapse={mockOnCollapse} />);

    const expandButton = screen.getByTitle("Expand Sidebar");
    fireEvent.click(expandButton);

    expect(mockOnCollapse).toHaveBeenCalledWith(false);
  });

  test("should not call onCollapse when onCollapse is not provided", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={false} />);

    const collapseButton = screen.getByTitle("Collapse Sidebar");
    fireEvent.click(collapseButton);

    expect(mockOnCollapse).not.toHaveBeenCalled();
  });

  test("should set active key to home for dashboard path", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar />);

    expect(screen.getByText("Home")).toBeInTheDocument();
  });

  test("should set active key to frontends for frontends path", () => {
    mockUsePathname.mockReturnValue("/dashboard/frontends");
    render(<Sidebar />);

    expect(screen.getByText("Frontends")).toBeInTheDocument();
  });

  test("should set active key to backends for backends path", () => {
    mockUsePathname.mockReturnValue("/dashboard/backends");
    render(<Sidebar />);

    expect(screen.getByText("Backends")).toBeInTheDocument();
  });

  test("should set active key to home for unknown path", () => {
    mockUsePathname.mockReturnValue("/unknown/path");
    render(<Sidebar />);

    expect(screen.getByText("Home")).toBeInTheDocument();
  });

  test("should render logo with full text when not collapsed", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={false} />);

    expect(screen.getByText("OllamaFlow")).toBeInTheDocument();
  });

  test("should render logo without text when collapsed", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={true} />);

    // The logo image should still be present
    const logoImage = screen.getByAltText("OllamaFlow");
    expect(logoImage).toBeInTheDocument();
  });

  test("should render all menu items with correct links", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar />);

    const homeLink = screen.getByText("Home").closest("a");
    const frontendsLink = screen.getByText("Frontends").closest("a");
    const backendsLink = screen.getByText("Backends").closest("a");

    expect(homeLink).toHaveAttribute("href", "/dashboard");
    expect(frontendsLink).toHaveAttribute("href", "/dashboard/frontends");
    expect(backendsLink).toHaveAttribute("href", "/dashboard/backends");
  });

  test("should render sidebar with custom collapsed state", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={true} onCollapse={mockOnCollapse} />);

    expect(screen.getByTitle("Expand Sidebar")).toBeInTheDocument();
  });

  test("should render sidebar with custom expanded state", () => {
    mockUsePathname.mockReturnValue("/dashboard");
    render(<Sidebar collapsed={false} onCollapse={mockOnCollapse} />);

    expect(screen.getByTitle("Collapse Sidebar")).toBeInTheDocument();
  });

  test("should handle frontends sub-paths correctly", () => {
    mockUsePathname.mockReturnValue("/dashboard/frontends/create");
    render(<Sidebar />);

    expect(screen.getByText("Frontends")).toBeInTheDocument();
  });

  test("should handle backends sub-paths correctly", () => {
    mockUsePathname.mockReturnValue("/dashboard/backends/edit/123");
    render(<Sidebar />);

    expect(screen.getByText("Backends")).toBeInTheDocument();
  });

  test("should render with all props combined", () => {
    mockUsePathname.mockReturnValue("/dashboard/frontends");
    render(<Sidebar collapsed={false} onCollapse={mockOnCollapse} />);

    expect(screen.getByText("Home")).toBeInTheDocument();
    expect(screen.getByText("Frontends")).toBeInTheDocument();
    expect(screen.getByText("Backends")).toBeInTheDocument();
    expect(screen.getByTitle("Collapse Sidebar")).toBeInTheDocument();
  });
});
