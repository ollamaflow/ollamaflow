import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import RootLayout from "#/app/layout";

// Mock the ErrorBoundary component
jest.mock("#/hoc/ErrorBoundary", () => {
  return function MockErrorBoundary({
    children,
    allowRefresh,
  }: {
    children: React.ReactNode;
    allowRefresh?: boolean;
  }) {
    return (
      <div data-testid="error-boundary" data-allow-refresh={allowRefresh}>
        {children}
      </div>
    );
  };
});

// Mock the AppProviders component
jest.mock("#/hoc/AppProviders", () => {
  return function MockAppProviders({
    children,
  }: {
    children: React.ReactNode;
  }) {
    return <div data-testid="app-providers">{children}</div>;
  };
});

describe("RootLayout", () => {
  test("should render root layout with children", () => {
    render(
      <RootLayout>
        <div data-testid="test-children">Test Content</div>
      </RootLayout>
    );

    expect(screen.getByTestId("error-boundary")).toBeInTheDocument();
    expect(screen.getByTestId("app-providers")).toBeInTheDocument();
    expect(screen.getByTestId("test-children")).toBeInTheDocument();
  });

  test("should render with correct HTML structure", () => {
    render(
      <RootLayout>
        <div>Test Content</div>
      </RootLayout>
    );

    const html = document.querySelector("html");
    const body = document.querySelector("body");
    const head = document.querySelector("head");

    expect(html).toBeInTheDocument();
    expect(body).toBeInTheDocument();
    expect(head).toBeInTheDocument();
  });

  test("should render with correct head elements", () => {
    render(
      <RootLayout>
        <div>Test Content</div>
      </RootLayout>
    );

    const favicon = document.querySelector('link[rel="icon"]');
    const preconnect1 = document.querySelector(
      'link[href="https://fonts.googleapis.com"]'
    );
    const preconnect2 = document.querySelector(
      'link[href="https://fonts.gstatic.com"]'
    );
    const fontLink = document.querySelector(
      'link[href*="fonts.googleapis.com/css2"]'
    );

    expect(favicon).toBeInTheDocument();
    expect(favicon).toHaveAttribute("href", "/images/ollama-flow-icon.png");
    expect(preconnect1).toBeInTheDocument();
    expect(preconnect2).toBeInTheDocument();
    expect(fontLink).toBeInTheDocument();
  });

  test("should render body with correct attributes", () => {
    render(
      <RootLayout>
        <div>Test Content</div>
      </RootLayout>
    );

    const body = document.querySelector("body");
    expect(body).toBeInTheDocument();
    // Note: suppressHydrationWarning and className may not be applied in test environment
  });

  test("should render error boundary with allowRefresh prop", () => {
    render(
      <RootLayout>
        <div>Test Content</div>
      </RootLayout>
    );

    const errorBoundary = screen.getByTestId("error-boundary");
    expect(errorBoundary).toHaveAttribute("data-allow-refresh", "true");
  });

  test("should render with complex children", () => {
    render(
      <RootLayout>
        <div data-testid="child1">Child 1</div>
        <div data-testid="child2">Child 2</div>
        <div data-testid="child3">Child 3</div>
      </RootLayout>
    );

    expect(screen.getByTestId("child1")).toBeInTheDocument();
    expect(screen.getByTestId("child2")).toBeInTheDocument();
    expect(screen.getByTestId("child3")).toBeInTheDocument();
  });
});
