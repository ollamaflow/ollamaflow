import "@testing-library/jest-dom";
import React from "react";
import { render, screen } from "@testing-library/react";
import Avatar from "#/components/base/avatar/Avatar";

describe("Avatar", () => {
  test("should render avatar with custom src", () => {
    const testSrc = "https://example.com/avatar.jpg";
    render(<Avatar src={testSrc} />);
    const avatar = screen.getByRole("img");
    expect(avatar).toHaveAttribute("src", testSrc);
  });

  test("should render avatar with children", () => {
    render(<Avatar>JD</Avatar>);
    const avatar = screen.getByText("JD");
    expect(avatar).toBeInTheDocument();
  });
});
