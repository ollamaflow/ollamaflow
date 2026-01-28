import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import Input from "#/components/base/input/Input";

describe("Input", () => {
  test("should render input with default props", () => {
    render(<Input />);
    const input = screen.getByRole("textbox");
    expect(input).toBeInTheDocument();
  });

  test("should render input with placeholder", () => {
    render(<Input placeholder="Enter text" />);
    const input = screen.getByPlaceholderText("Enter text");
    expect(input).toBeInTheDocument();
  });

  test("should render input with value", () => {
    render(<Input value="test value" />);
    const input = screen.getByDisplayValue("test value");
    expect(input).toBeInTheDocument();
  });

  test("should render disabled input", () => {
    render(<Input disabled />);
    const input = screen.getByRole("textbox");
    expect(input).toBeDisabled();
  });

  test("should render input with custom className", () => {
    const customClass = "custom-input-class";
    render(<Input className={customClass} />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveClass(customClass);
  });

  test("should handle input change", () => {
    const handleChange = jest.fn();
    render(<Input onChange={handleChange} />);

    const input = screen.getByRole("textbox");
    fireEvent.change(input, { target: { value: "new value" } });

    expect(handleChange).toHaveBeenCalled();
  });

  test("should render input with prefix", () => {
    render(<Input prefix={<span>@</span>} />);
    expect(screen.getByText("@")).toBeInTheDocument();
  });

  test("should render input with suffix", () => {
    render(<Input suffix={<span>USD</span>} />);
    expect(screen.getByText("USD")).toBeInTheDocument();
  });

  test("should render input with size", () => {
    render(<Input size="large" />);
    const input = screen.getByRole("textbox");
    expect(input).toBeInTheDocument();
  });

  test("should render input with maxLength", () => {
    render(<Input maxLength={10} />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveAttribute("maxLength", "10");
  });

  test("should render input with autoComplete", () => {
    render(<Input autoComplete="off" />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveAttribute("autoComplete", "off");
  });
});
