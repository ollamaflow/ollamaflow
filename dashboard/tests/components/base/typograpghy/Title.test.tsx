import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import Title from "#/components/base/typograpghy/Title";

describe("Title", () => {
  test("should render title with children", () => {
    render(<Title>This is a title</Title>);
    expect(screen.getByText("This is a title")).toBeInTheDocument();
  });

  test("should render title with level", () => {
    render(<Title level={1}>H1 Title</Title>);
    expect(screen.getByText("H1 Title")).toBeInTheDocument();
  });

  test("should render title with custom className", () => {
    const customClass = "custom-title-class";
    render(<Title className={customClass}>Custom title</Title>);
    const title = screen.getByText("Custom title");
    expect(title).toHaveClass(customClass);
  });

  test("should render title with copyable prop", () => {
    render(<Title copyable>Copyable title</Title>);
    expect(screen.getByText("Copyable title")).toBeInTheDocument();
  });

  test("should render title with ellipsis", () => {
    render(<Title ellipsis>Long title text that should be truncated</Title>);
    expect(
      screen.getByText("Long title text that should be truncated")
    ).toBeInTheDocument();
  });

  test("should render title with strong prop", () => {
    render(<Title strong>Strong title</Title>);
    expect(screen.getByText("Strong title")).toBeInTheDocument();
  });

  test("should render title with italic prop", () => {
    render(<Title italic>Italic title</Title>);
    expect(screen.getByText("Italic title")).toBeInTheDocument();
  });

  test("should render title with underline prop", () => {
    render(<Title underline>Underlined title</Title>);
    expect(screen.getByText("Underlined title")).toBeInTheDocument();
  });

  test("should render title with delete prop", () => {
    render(<Title delete>Deleted title</Title>);
    expect(screen.getByText("Deleted title")).toBeInTheDocument();
  });

  test("should render title with mark prop", () => {
    render(<Title mark>Marked title</Title>);
    expect(screen.getByText("Marked title")).toBeInTheDocument();
  });

  test("should render title with code prop", () => {
    render(<Title code>Code title</Title>);
    expect(screen.getByText("Code title")).toBeInTheDocument();
  });

  test("should render title with keyboard prop", () => {
    render(<Title keyboard>Keyboard title</Title>);
    expect(screen.getByText("Keyboard title")).toBeInTheDocument();
  });

  test("should render title with type", () => {
    render(<Title type="secondary">Secondary title</Title>);
    expect(screen.getByText("Secondary title")).toBeInTheDocument();
  });

  test("should render title with disabled prop", () => {
    render(<Title disabled>Disabled title</Title>);
    expect(screen.getByText("Disabled title")).toBeInTheDocument();
  });

  test("should render title with different levels", () => {
    render(<Title level={2}>H2 Title</Title>);
    expect(screen.getByText("H2 Title")).toBeInTheDocument();
  });

  test("should render title with level 3", () => {
    render(<Title level={3}>H3 Title</Title>);
    expect(screen.getByText("H3 Title")).toBeInTheDocument();
  });

  test("should render title with level 4", () => {
    render(<Title level={4}>H4 Title</Title>);
    expect(screen.getByText("H4 Title")).toBeInTheDocument();
  });

  test("should render title with level 5", () => {
    render(<Title level={5}>H5 Title</Title>);
    expect(screen.getByText("H5 Title")).toBeInTheDocument();
  });
});
