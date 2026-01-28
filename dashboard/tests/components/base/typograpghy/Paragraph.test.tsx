import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import Paragraph from "#/components/base/typograpghy/Paragraph";

describe("Paragraph", () => {
  test("should render paragraph with children", () => {
    render(<Paragraph>This is a paragraph</Paragraph>);
    expect(screen.getByText("This is a paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with custom className", () => {
    const customClass = "custom-paragraph-class";
    render(<Paragraph className={customClass}>Custom paragraph</Paragraph>);
    const paragraph = screen.getByText("Custom paragraph");
    expect(paragraph).toHaveClass(customClass);
  });

  test("should render paragraph with copyable prop", () => {
    render(<Paragraph copyable>Copyable paragraph</Paragraph>);
    expect(screen.getByText("Copyable paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with ellipsis", () => {
    render(
      <Paragraph ellipsis>
        Long paragraph text that should be truncated
      </Paragraph>
    );
    expect(
      screen.getByText("Long paragraph text that should be truncated")
    ).toBeInTheDocument();
  });

  test("should render paragraph with strong prop", () => {
    render(<Paragraph strong>Strong paragraph</Paragraph>);
    expect(screen.getByText("Strong paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with italic prop", () => {
    render(<Paragraph italic>Italic paragraph</Paragraph>);
    expect(screen.getByText("Italic paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with underline prop", () => {
    render(<Paragraph underline>Underlined paragraph</Paragraph>);
    expect(screen.getByText("Underlined paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with delete prop", () => {
    render(<Paragraph delete>Deleted paragraph</Paragraph>);
    expect(screen.getByText("Deleted paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with mark prop", () => {
    render(<Paragraph mark>Marked paragraph</Paragraph>);
    expect(screen.getByText("Marked paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with code prop", () => {
    render(<Paragraph code>Code paragraph</Paragraph>);
    expect(screen.getByText("Code paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with keyboard prop", () => {
    render(<Paragraph keyboard>Keyboard paragraph</Paragraph>);
    expect(screen.getByText("Keyboard paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with type", () => {
    render(<Paragraph type="secondary">Secondary paragraph</Paragraph>);
    expect(screen.getByText("Secondary paragraph")).toBeInTheDocument();
  });

  test("should render paragraph with disabled prop", () => {
    render(<Paragraph disabled>Disabled paragraph</Paragraph>);
    expect(screen.getByText("Disabled paragraph")).toBeInTheDocument();
  });
});
