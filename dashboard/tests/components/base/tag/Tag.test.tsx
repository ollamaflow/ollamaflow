import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import OllamaFlowTag from "#/components/base/tag/Tag";

describe("OllamaFlowTag", () => {
  test("should render tag with label", () => {
    render(<OllamaFlowTag label="Test Tag" />);

    expect(screen.getByText("Test Tag")).toBeInTheDocument();
  });

  test("should render tag with color", () => {
    render(<OllamaFlowTag label="Colored Tag" color="blue" />);

    expect(screen.getByText("Colored Tag")).toBeInTheDocument();
  });

  test("should render tag with icon", () => {
    const icon = <span data-testid="tag-icon">🚀</span>;
    render(<OllamaFlowTag label="Tag with Icon" icon={icon} />);

    expect(screen.getByText("Tag with Icon")).toBeInTheDocument();
    expect(screen.getByTestId("tag-icon")).toBeInTheDocument();
  });

  test("should render closable tag", () => {
    const onClose = jest.fn();
    render(<OllamaFlowTag label="Closable Tag" closable onClose={onClose} />);

    expect(screen.getByText("Closable Tag")).toBeInTheDocument();
    expect(screen.queryByRole("icon")).not.toBeInTheDocument();
  });

  //   test('should call onClose when close button is clicked', () => {
  //     const onClose = jest.fn();
  //     render(<OllamaFlowTag label="Closable Tag" closable onClose={onClose} />);

  //     const closeButton = screen.getByRole('icon');
  //     fireEvent.click(closeButton);

  //     expect(onClose).toHaveBeenCalledTimes(1);
  //   });

  test("should render tag with custom className", () => {
    const customClass = "custom-tag-class";
    const { container } = render(
      <OllamaFlowTag label="Custom Tag" className={customClass} />
    );

    expect(container.firstChild).toHaveClass(customClass);
  });

  test("should render tag with custom style", () => {
    const customStyle = { backgroundColor: "red", color: "white" };
    render(<OllamaFlowTag label="Styled Tag" style={customStyle} />);

    expect(screen.getByText("Styled Tag")).toBeInTheDocument();
  });

  test("should render tag with bordered prop", () => {
    render(<OllamaFlowTag label="Bordered Tag" bordered={false} />);

    expect(screen.getByText("Bordered Tag")).toBeInTheDocument();
  });

  test("should render tag with checkable prop", () => {
    const onCheck = jest.fn();
    render(<OllamaFlowTag label="Checkable Tag" checkable onCheck={onCheck} />);

    expect(screen.getByText("Checkable Tag")).toBeInTheDocument();
  });

  test("should render tag with checked state", () => {
    render(<OllamaFlowTag label="Checked Tag" checked />);

    expect(screen.getByText("Checked Tag")).toBeInTheDocument();
  });

  test("should render tag with different colors", () => {
    const colors = ["red", "blue", "green", "orange", "purple"];

    colors.forEach((color) => {
      const { unmount } = render(
        <OllamaFlowTag label={`${color} tag`} color={color} />
      );
      expect(screen.getByText(`${color} tag`)).toBeInTheDocument();
      unmount();
    });
  });

  test("should render tag with long label", () => {
    const longLabel =
      "This is a very long tag label that should be displayed properly";
    render(<OllamaFlowTag label={longLabel} />);

    expect(screen.getByText(longLabel)).toBeInTheDocument();
  });

  test("should render tag with special characters in label", () => {
    const specialLabel = "Tag with special chars: !@#$%^&*()";
    render(<OllamaFlowTag label={specialLabel} />);

    expect(screen.getByText(specialLabel)).toBeInTheDocument();
  });
});
