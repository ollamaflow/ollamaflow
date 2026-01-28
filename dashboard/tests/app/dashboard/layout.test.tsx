import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import DashboardLayoutComponent from "#/app/dashboard/layout";

// Mock the DashboardLayout component
jest.mock("#/components/layout/DashboardLayout", () => {
  return function MockDashboardLayout({
    children,
  }: {
    children: React.ReactNode;
  }) {
    return <div data-testid="dashboard-layout">{children}</div>;
  };
});

// Mock the withAuth HOC
jest.mock("#/hoc/hoc", () => ({
  withAuth: (Component: React.ComponentType<any>) => {
    return function WithAuthComponent(props: any) {
      return <Component {...props} />;
    };
  },
}));

describe("DashboardLayout", () => {
  test("should render dashboard layout with children", () => {
    render(
      <DashboardLayoutComponent>
        <div data-testid="test-children">Dashboard Content</div>
      </DashboardLayoutComponent>
    );

    expect(screen.getByTestId("dashboard-layout")).toBeInTheDocument();
    expect(screen.getByTestId("test-children")).toBeInTheDocument();
    expect(screen.getByText("Dashboard Content")).toBeInTheDocument();
  });

  test("should render with multiple children", () => {
    render(
      <DashboardLayoutComponent>
        <div data-testid="child1">Child 1</div>
        <div data-testid="child2">Child 2</div>
        <div data-testid="child3">Child 3</div>
      </DashboardLayoutComponent>
    );

    expect(screen.getByTestId("dashboard-layout")).toBeInTheDocument();
    expect(screen.getByTestId("child1")).toBeInTheDocument();
    expect(screen.getByTestId("child2")).toBeInTheDocument();
    expect(screen.getByTestId("child3")).toBeInTheDocument();
  });

  test("should render with empty children", () => {
    render(<DashboardLayoutComponent>{}</DashboardLayoutComponent>);

    expect(screen.getByTestId("dashboard-layout")).toBeInTheDocument();
  });

  test("should render with null children", () => {
    render(<DashboardLayoutComponent>{null}</DashboardLayoutComponent>);

    expect(screen.getByTestId("dashboard-layout")).toBeInTheDocument();
  });

  test("should render with undefined children", () => {
    render(<DashboardLayoutComponent>{undefined}</DashboardLayoutComponent>);

    expect(screen.getByTestId("dashboard-layout")).toBeInTheDocument();
  });
});
