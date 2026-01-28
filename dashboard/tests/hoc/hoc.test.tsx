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
  useAppSelector: jest.fn(),
}));

// Mock Next.js navigation
jest.mock("next/navigation", () => ({
  useRouter: jest.fn(),
}));

// Mock the PageLoading component
jest.mock("#/components/base/loading/PageLoading", () => {
  return function MockPageLoading({ className }: { className?: string }) {
    return (
      <div data-testid="page-loading" className={className}>
        Loading...
      </div>
    );
  };
});

// Mock the LogoutFallBack component
jest.mock("#/components/logout-fallback/LogoutFallBack", () => {
  return function MockLogoutFallBack({ logoutPath }: { logoutPath: string }) {
    return (
      <div data-testid="logout-fallback" data-logout-path={logoutPath}>
        Logout Fallback
      </div>
    );
  };
});

// Mock the constants
jest.mock("#/constants/constant", () => ({
  paths: {
    Login: "/login",
    Dashboard: "/dashboard",
  },
}));

// Import after mocks
import { withAuth, forGuest } from "#/hoc/hoc";

describe("HOC Components", () => {
  const mockUseAppSelector = require("#/lib/store/hooks").useAppSelector;
  const mockUseRouter = require("next/navigation").useRouter;

  beforeEach(() => {
    jest.clearAllMocks();
    // Reset useState to return null initially for loading state
    require("react").useState.mockImplementation((initialValue) => [
      initialValue,
      mockSetState,
    ]);
    // Reset useEffect mock to execute callbacks
    require("react").useEffect.mockImplementation((callback, deps) => {
      mockUseEffect(callback, deps);
      callback();
    });
  });

  describe("withAuth", () => {
    const TestComponent = () => (
      <div data-testid="test-component">Test Component</div>
    );
    const WithAuthComponent = withAuth(TestComponent);

    test("should render loading state initially", () => {
      // Mock useState to return null initially (loading state)
      require("react").useState.mockImplementation(() => [null, mockSetState]);
      mockUseAppSelector.mockReturnValue(null);

      render(<WithAuthComponent />);

      expect(screen.getByTestId("page-loading")).toBeInTheDocument();
      expect(screen.queryByTestId("test-component")).not.toBeInTheDocument();
      expect(screen.queryByTestId("logout-fallback")).not.toBeInTheDocument();
    });

    test("should render wrapped component when auth is valid", async () => {
      // Mock useState to return true (valid auth)
      require("react").useState.mockImplementation(() => [true, mockSetState]);
      mockUseAppSelector.mockReturnValue("valid-access-key");

      render(<WithAuthComponent />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();
      expect(screen.queryByTestId("page-loading")).not.toBeInTheDocument();
      expect(screen.queryByTestId("logout-fallback")).not.toBeInTheDocument();
    });

    test("should render logout fallback when auth is invalid", async () => {
      // Mock useState to return false (invalid auth)
      require("react").useState.mockImplementation(() => [false, mockSetState]);
      mockUseAppSelector.mockReturnValue(null);

      render(<WithAuthComponent />);

      expect(screen.getByTestId("logout-fallback")).toBeInTheDocument();
      expect(screen.getByTestId("logout-fallback")).toHaveAttribute(
        "data-logout-path",
        "/login"
      );
      expect(screen.queryByTestId("test-component")).not.toBeInTheDocument();
      expect(screen.queryByTestId("page-loading")).not.toBeInTheDocument();
    });

    test("should pass props to wrapped component", async () => {
      // Mock useState to return true (valid auth)
      require("react").useState.mockImplementation(() => [true, mockSetState]);
      mockUseAppSelector.mockReturnValue("valid-access-key");

      const TestComponentWithProps = ({
        title,
        count,
      }: {
        title: string;
        count: number;
      }) => (
        <div data-testid="test-component">
          {title}: {count}
        </div>
      );
      const WithAuthComponentWithProps = withAuth(TestComponentWithProps);

      render(<WithAuthComponentWithProps title="Test" count={5} />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();
      expect(screen.getByText("Test: 5")).toBeInTheDocument();
    });

    test("should handle empty access key", async () => {
      // Mock useState to return false (invalid auth)
      require("react").useState.mockImplementation(() => [false, mockSetState]);
      mockUseAppSelector.mockReturnValue("");

      render(<WithAuthComponent />);

      expect(screen.getByTestId("logout-fallback")).toBeInTheDocument();
      expect(screen.queryByTestId("test-component")).not.toBeInTheDocument();
    });

    test("should handle undefined access key", async () => {
      // Mock useState to return false (invalid auth)
      require("react").useState.mockImplementation(() => [false, mockSetState]);
      mockUseAppSelector.mockReturnValue(undefined);

      render(<WithAuthComponent />);

      expect(screen.getByTestId("logout-fallback")).toBeInTheDocument();
      expect(screen.queryByTestId("test-component")).not.toBeInTheDocument();
    });

    test("should render loading with correct className", () => {
      // Mock useState to return null (loading state)
      require("react").useState.mockImplementation(() => [null, mockSetState]);
      mockUseAppSelector.mockReturnValue(null);

      render(<WithAuthComponent />);

      const loadingElement = screen.getByTestId("page-loading");
      expect(loadingElement).toHaveClass("h-100vh");
    });
  });

  describe("forGuest", () => {
    const TestComponent = () => (
      <div data-testid="test-component">Guest Component</div>
    );
    const ForGuestComponent = forGuest(TestComponent);

    test("should render wrapped component when no auth", () => {
      mockUseAppSelector.mockReturnValue(null);
      mockUseRouter.mockReturnValue({ push: jest.fn() });

      render(<ForGuestComponent />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();
      expect(mockUseRouter().push).not.toHaveBeenCalled();
    });

    test("should not render component and redirect when auth exists", () => {
      mockUseAppSelector.mockReturnValue("valid-access-key");
      const mockPush = jest.fn();
      mockUseRouter.mockReturnValue({ push: mockPush });

      render(<ForGuestComponent />);

      // The component should not render anything when auth exists
      expect(screen.queryByTestId("test-component")).not.toBeInTheDocument();
      expect(mockPush).toHaveBeenCalledWith("/dashboard");
    });

    test("should pass props to wrapped component", () => {
      mockUseAppSelector.mockReturnValue(null);
      mockUseRouter.mockReturnValue({ push: jest.fn() });

      const TestComponentWithProps = ({
        title,
        count,
      }: {
        title: string;
        count: number;
      }) => (
        <div data-testid="test-component">
          {title}: {count}
        </div>
      );
      const ForGuestComponentWithProps = forGuest(TestComponentWithProps);

      render(<ForGuestComponentWithProps title="Guest" count={10} />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();
      expect(screen.getByText("Guest: 10")).toBeInTheDocument();
    });

    test("should handle empty access key", () => {
      mockUseAppSelector.mockReturnValue("");
      mockUseRouter.mockReturnValue({ push: jest.fn() });

      render(<ForGuestComponent />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();
      expect(mockUseRouter().push).not.toHaveBeenCalled();
    });

    test("should handle undefined access key", () => {
      mockUseAppSelector.mockReturnValue(undefined);
      mockUseRouter.mockReturnValue({ push: jest.fn() });

      render(<ForGuestComponent />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();
      expect(mockUseRouter().push).not.toHaveBeenCalled();
    });

    test("should redirect when auth changes from null to valid", () => {
      mockUseAppSelector.mockReturnValue(null);
      const mockPush = jest.fn();
      mockUseRouter.mockReturnValue({ push: mockPush });

      const { rerender } = render(<ForGuestComponent />);

      expect(screen.getByTestId("test-component")).toBeInTheDocument();

      // Change auth to valid
      mockUseAppSelector.mockReturnValue("valid-access-key");
      rerender(<ForGuestComponent />);

      expect(mockPush).toHaveBeenCalledWith("/dashboard");
    });

    test("should not redirect when auth changes from valid to null", () => {
      mockUseAppSelector.mockReturnValue("valid-access-key");
      const mockPush = jest.fn();
      mockUseRouter.mockReturnValue({ push: mockPush });

      const { rerender } = render(<ForGuestComponent />);

      // Should redirect immediately when auth is valid
      expect(mockPush).toHaveBeenCalledWith("/dashboard");

      // Change auth to null
      mockUseAppSelector.mockReturnValue(null);
      rerender(<ForGuestComponent />);

      // Should not call push again
      expect(mockPush).toHaveBeenCalledTimes(1);
    });

    test("should handle complex wrapped component", () => {
      mockUseAppSelector.mockReturnValue(null);
      mockUseRouter.mockReturnValue({ push: jest.fn() });

      const ComplexComponent = () => (
        <div>
          <h1>Guest Title</h1>
          <p>Guest content with multiple elements</p>
          <button>Guest Button</button>
        </div>
      );
      const ForGuestComplexComponent = forGuest(ComplexComponent);

      render(<ForGuestComplexComponent />);

      expect(screen.getByText("Guest Title")).toBeInTheDocument();
      expect(
        screen.getByText("Guest content with multiple elements")
      ).toBeInTheDocument();
      expect(screen.getByText("Guest Button")).toBeInTheDocument();
    });

    test("should handle wrapped component with children", () => {
      mockUseAppSelector.mockReturnValue(null);
      mockUseRouter.mockReturnValue({ push: jest.fn() });

      const ComponentWithChildren = ({
        children,
      }: {
        children: React.ReactNode;
      }) => (
        <div>
          <div>Guest Component</div>
          {children}
        </div>
      );
      const ForGuestComponentWithChildren = forGuest(ComponentWithChildren);

      render(
        <ForGuestComponentWithChildren>
          <div>Child content</div>
        </ForGuestComponentWithChildren>
      );

      expect(screen.getByText("Guest Component")).toBeInTheDocument();
      expect(screen.getByText("Child content")).toBeInTheDocument();
    });
  });
});
