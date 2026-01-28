import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import Card from "#/components/base/card/Card";

describe("Card", () => {
  test("should render card with children", () => {
    render(
      <Card>
        <div>Card Content</div>
      </Card>
    );
    expect(screen.getByText("Card Content")).toBeInTheDocument();
  });

  test("should render card with title", () => {
    render(
      <Card title="Test Card">
        <div>Card Content</div>
      </Card>
    );
    expect(screen.getByText("Test Card")).toBeInTheDocument();
    expect(screen.getByText("Card Content")).toBeInTheDocument();
  });

  test("should render card with custom className", () => {
    const customClass = "custom-card-class";
    render(
      <Card className={customClass}>
        <div>Card Content</div>
      </Card>
    );
    const card = screen.getByText("Card Content").closest(".ant-card");
    expect(card).toHaveClass(customClass);
  });

  test("should render card with extra content", () => {
    render(
      <Card extra={<button>Extra Button</button>}>
        <div>Card Content</div>
      </Card>
    );
    expect(screen.getByText("Card Content")).toBeInTheDocument();
    expect(screen.getByText("Extra Button")).toBeInTheDocument();
  });

  test("should render card with bordered prop", () => {
    render(
      <Card>
        <div>Card Content</div>
      </Card>
    );
    expect(screen.getByText("Card Content")).toBeInTheDocument();
  });
});
