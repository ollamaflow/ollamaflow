import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import Divider from "#/components/base/divider/Divider";

describe("Divider", () => {
  test("should render divider with default props", () => {
    render(<Divider />);
    const divider = screen.getByRole("separator");
    expect(divider).toBeInTheDocument();
  });

  test("should render divider with text", () => {
    render(<Divider>Divider Text</Divider>);
    expect(screen.getByText("Divider Text")).toBeInTheDocument();
  });

  test("should render horizontal divider", () => {
    render(<Divider type="horizontal" />);
    const divider = screen.getByRole("separator");
    expect(divider).toBeInTheDocument();
  });

  test("should render vertical divider", () => {
    render(<Divider type="vertical" />);
    const divider = screen.getByRole("separator");
    expect(divider).toBeInTheDocument();
  });

  test("should render dashed divider", () => {
    render(<Divider dashed />);
    const divider = screen.getByRole("separator");
    expect(divider).toBeInTheDocument();
  });

  test("should render divider with custom orientation", () => {
    render(<Divider orientation="left">Left Text</Divider>);
    expect(screen.getByText("Left Text")).toBeInTheDocument();
  });

  test("should render divider with custom className", () => {
    const customClass = "custom-divider-class";
    render(<Divider className={customClass} />);
    const divider = screen.getByRole("separator");
    expect(divider).toHaveClass(customClass);
  });

  test("should render plain divider", () => {
    render(<Divider plain>Plain Text</Divider>);
    expect(screen.getByText("Plain Text")).toBeInTheDocument();
  });
});
