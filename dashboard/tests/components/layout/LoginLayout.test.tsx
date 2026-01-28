import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import LoginLayout from "#/components/layout/LoginLayout";

// Mock the logo component
jest.mock("#/components/logo/OllamaFlowLogo", () => {
  return function MockLogo() {
    return <div data-testid="logo">OllamaFlow Logo</div>;
  };
});

// Mock the theme mode switch component
jest.mock("#/components/theme-mode-switch/ThemeModeSwitch", () => {
  return function MockThemeModeSwitch() {
    return <div data-testid="theme-mode-switch">Theme Switch</div>;
  };
});

// Mock the title component
jest.mock("#/components/base/typograpghy/Title", () => {
  return function MockTitle({
    children,
    fontSize,
    weight,
    style,
  }: {
    children: React.ReactNode;
    fontSize?: number;
    weight?: number;
    style?: React.CSSProperties;
  }) {
    const combinedStyle: React.CSSProperties = {};

    if (fontSize !== undefined) {
      combinedStyle.fontSize = `${fontSize}px`;
    }
    if (weight !== undefined) {
      combinedStyle.fontWeight = weight;
    }
    if (style) {
      Object.assign(combinedStyle, style);
    }

    return (
      <h1 data-testid="title" style={combinedStyle}>
        {children}
      </h1>
    );
  };
});

// Mock the paragraph component
jest.mock("#/components/base/typograpghy/Paragraph", () => {
  return function MockParagraph({
    children,
    className,
  }: {
    children: React.ReactNode;
    className?: string;
  }) {
    return (
      <p data-testid="paragraph" className={className}>
        {children}
      </p>
    );
  };
});

describe("LoginLayout", () => {
  test("should render login layout with children", () => {
    render(
      <LoginLayout>
        <div data-testid="login-form">Login Form</div>
      </LoginLayout>
    );

    expect(screen.getByTestId("logo")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("title")).toBeInTheDocument();
    expect(screen.getByTestId("paragraph")).toBeInTheDocument();
    expect(screen.getByTestId("login-form")).toBeInTheDocument();
  });

  test("should render with default login title", () => {
    render(
      <LoginLayout>
        <div>Login Form</div>
      </LoginLayout>
    );

    expect(screen.getByText("Login")).toBeInTheDocument();
  });

  test("should render with admin login title when isAdmin is true", () => {
    render(
      <LoginLayout isAdmin={true}>
        <div>Login Form</div>
      </LoginLayout>
    );

    expect(screen.getByText("Admin Login")).toBeInTheDocument();
  });

  test("should render with default login description", () => {
    render(
      <LoginLayout>
        <div>Login Form</div>
      </LoginLayout>
    );

    expect(
      screen.getByText("Please enter your email and password to login")
    ).toBeInTheDocument();
  });

  test("should render with admin login description when isAdmin is true", () => {
    render(
      <LoginLayout isAdmin={true}>
        <div>Login Form</div>
      </LoginLayout>
    );

    expect(
      screen.getByText("Please enter your access key to login")
    ).toBeInTheDocument();
  });

  test("should render with complex children", () => {
    const complexChildren = (
      <div>
        <input type="email" placeholder="Email" />
        <input type="password" placeholder="Password" />
        <button type="submit">Login</button>
      </div>
    );

    render(<LoginLayout>{complexChildren}</LoginLayout>);

    expect(screen.getByPlaceholderText("Email")).toBeInTheDocument();
    expect(screen.getByPlaceholderText("Password")).toBeInTheDocument();
    const loginElements = screen.getAllByText("Login");
    const loginButton = loginElements.find(
      (element) => element.tagName === "BUTTON"
    );
    expect(loginButton).toBeInTheDocument();
  });

  test("should render with empty children", () => {
    render(<LoginLayout>{}</LoginLayout>);

    expect(screen.getByTestId("logo")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("title")).toBeInTheDocument();
    expect(screen.getByTestId("paragraph")).toBeInTheDocument();
  });

  test("should render with null children", () => {
    render(<LoginLayout>{null}</LoginLayout>);

    expect(screen.getByTestId("logo")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("title")).toBeInTheDocument();
    expect(screen.getByTestId("paragraph")).toBeInTheDocument();
  });

  test("should render with undefined children", () => {
    render(<LoginLayout>{undefined}</LoginLayout>);

    expect(screen.getByTestId("logo")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByTestId("title")).toBeInTheDocument();
    expect(screen.getByTestId("paragraph")).toBeInTheDocument();
  });

  test("should render with multiple children", () => {
    render(
      <LoginLayout>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </LoginLayout>
    );

    expect(screen.getByText("Child 1")).toBeInTheDocument();
    expect(screen.getByText("Child 2")).toBeInTheDocument();
    expect(screen.getByText("Child 3")).toBeInTheDocument();
  });

  test("should render title with correct font size and weight", () => {
    render(
      <LoginLayout>
        <div>Login Form</div>
      </LoginLayout>
    );

    const titles = screen.getAllByTestId("title");
    const loginTitle = titles.find((title) => title.textContent === "Login");
    expect(loginTitle).toBeInTheDocument();

    expect(loginTitle).toHaveAttribute("style");
    const styleAttribute = loginTitle?.getAttribute("style");
    expect(styleAttribute).toContain("font-size: 22px");
    expect(styleAttribute).toContain("font-weight: 600");
  });

  test("should render admin title with correct font size and weight", () => {
    render(
      <LoginLayout isAdmin={true}>
        <div>Login Form</div>
      </LoginLayout>
    );

    const titles = screen.getAllByTestId("title");
    const adminTitle = titles.find(
      (title) => title.textContent === "Admin Login"
    );
    expect(adminTitle).toBeInTheDocument();

    expect(adminTitle).toHaveAttribute("style");
    const styleAttribute = adminTitle?.getAttribute("style");
    expect(styleAttribute).toContain("font-size: 22px");
    expect(styleAttribute).toContain("font-weight: 600");
  });

  test("should render paragraph with correct className", () => {
    render(
      <LoginLayout>
        <div>Login Form</div>
      </LoginLayout>
    );

    const paragraph = screen.getByTestId("paragraph");
    expect(paragraph).toHaveClass("loginDescription");
  });

  test("should render with all props combined", () => {
    render(
      <LoginLayout isAdmin={true}>
        <div data-testid="admin-login-form">Admin Login Form</div>
      </LoginLayout>
    );

    expect(screen.getByTestId("logo")).toBeInTheDocument();
    expect(screen.getByTestId("theme-mode-switch")).toBeInTheDocument();
    expect(screen.getByText("Admin Login")).toBeInTheDocument();
    expect(
      screen.getByText("Please enter your access key to login")
    ).toBeInTheDocument();
    expect(screen.getByTestId("admin-login-form")).toBeInTheDocument();
  });

  test("should render with isAdmin as false", () => {
    render(
      <LoginLayout isAdmin={false}>
        <div>Login Form</div>
      </LoginLayout>
    );

    expect(screen.getByText("Login")).toBeInTheDocument();
    expect(
      screen.getByText("Please enter your email and password to login")
    ).toBeInTheDocument();
  });

  test("should render with isAdmin as undefined", () => {
    render(
      <LoginLayout>
        <div>Login Form</div>
      </LoginLayout>
    );

    expect(screen.getByText("Login")).toBeInTheDocument();
    expect(
      screen.getByText("Please enter your email and password to login")
    ).toBeInTheDocument();
  });
});
