import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import FormItem from "#/components/base/form/FormItem";

describe("FormItem", () => {
  test("should render form item with label", () => {
    render(
      <FormItem label="Test Label">
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
  });

  test("should render form item with required label", () => {
    render(
      <FormItem label="Test Label" required>
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
  });

  test("should render form item with help text", () => {
    render(
      <FormItem label="Test Label" help="Help text">
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
    expect(screen.getByText("Help text")).toBeInTheDocument();
  });

  test("should render form item with validation error", () => {
    render(
      <FormItem label="Test Label" validateStatus="error" help="Error message">
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
    expect(screen.getByText("Error message")).toBeInTheDocument();
  });

  test("should render form item with success status", () => {
    render(
      <FormItem label="Test Label" validateStatus="success">
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
  });

  test("should render form item with warning status", () => {
    render(
      <FormItem label="Test Label" validateStatus="warning">
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
  });

  test("should render form item with custom className", () => {
    const customClass = "custom-form-item-class";
    render(
      <FormItem label="Test Label" className={customClass}>
        <input type="text" />
      </FormItem>
    );
    expect(screen.getByText("Test Label")).toBeInTheDocument();
  });

  test("should render form item without label", () => {
    render(
      <FormItem>
        <input type="text" />
      </FormItem>
    );
    const input = screen.getByRole("textbox");
    expect(input).toBeInTheDocument();
  });
});
