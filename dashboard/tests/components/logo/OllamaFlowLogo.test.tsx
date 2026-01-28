import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import OllamaFlowLogo from "#/components/logo/OllamaFlowLogo";

// Mock the Flex component
jest.mock("#/components/base/flex/Flex", () => {
  return function MockFlex({
    children,
    align,
    gap,
    className,
  }: {
    children: React.ReactNode;
    align?: string;
    gap?: number;
    className?: string;
  }) {
    return (
      <div
        data-testid="flex"
        style={{ display: "flex", alignItems: align, gap: gap }}
        className={className}
      >
        {children}
      </div>
    );
  };
});

// Mock the Text component
jest.mock("#/components/base/typograpghy/Text", () => {
  return function MockText({
    children,
    fontSize,
    weight,
  }: {
    children: React.ReactNode;
    fontSize?: number;
    weight?: number;
  }) {
    return (
      <span
        data-testid="text"
        style={{ fontSize: `${fontSize}px`, fontWeight: weight }}
      >
        {children}
      </span>
    );
  };
});

describe("OllamaFlowLogo", () => {
  test("should render logo with image and text", () => {
    render(<OllamaFlowLogo />);

    expect(screen.getByTestId("flex")).toBeInTheDocument();
    expect(screen.getByTestId("text")).toBeInTheDocument();
    expect(screen.getByAltText("OllamaFlow")).toBeInTheDocument();
    expect(screen.getByText("OllamaFlow")).toBeInTheDocument();
  });

  test("should render image with correct attributes", () => {
    render(<OllamaFlowLogo />);

    const image = screen.getByAltText("OllamaFlow");
    expect(image).toHaveAttribute("src", "/images/ollama-flow-icon.png");
    expect(image).toHaveAttribute("height", "40");
  });

  test("should render text with correct styling", () => {
    render(<OllamaFlowLogo />);

    const text = screen.getByTestId("text");
    const styleAttribute = text.getAttribute("style");
    expect(styleAttribute).toContain("font-size: 20px");
    expect(styleAttribute).toContain("font-weight: 600");
    expect(text).toHaveTextContent("OllamaFlow");
  });

  test("should render flex container with correct props", () => {
    render(<OllamaFlowLogo />);

    const flex = screen.getByTestId("flex");
    const styleAttribute = flex.getAttribute("style");
    expect(styleAttribute).toContain("display: flex");
    expect(styleAttribute).toContain("align-items: center");
    expect(styleAttribute).toContain("gap: 7px");
  });

  test("should have fade-in class on flex container", () => {
    render(<OllamaFlowLogo />);

    const flex = screen.getByTestId("flex");
    expect(flex).toHaveClass("fade-in");
  });
});
