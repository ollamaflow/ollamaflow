import "@testing-library/jest-dom";
import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import LandingScreen from "#/page/home/LandingScreen";
import { paths } from "#/constants/constant";

// Mock Next.js Link component
jest.mock("next/link", () => {
  return function MockLink({
    children,
    href,
  }: {
    children: React.ReactNode;
    href: string;
  }) {
    return <a href={href}>{children}</a>;
  };
});

// Mock the card component
jest.mock("#/components/base/card/Card", () => {
  return function MockCard({
    children,
    onClick,
    className,
    hoverable,
  }: {
    children: React.ReactNode;
    onClick?: () => void;
    className?: string;
    hoverable?: boolean;
  }) {
    return (
      <div data-testid="card" className={className} onClick={onClick}>
        {children}
      </div>
    );
  };
});

// Mock the button component
jest.mock("#/components/base/button/Button", () => {
  return function MockButton({
    children,
    type,
    size,
    icon,
  }: {
    children: React.ReactNode;
    type?: string;
    size?: string;
    icon?: React.ReactNode;
  }) {
    return (
      <button data-testid="button" data-type={type} data-size={size}>
        {icon}
        {children}
      </button>
    );
  };
});

// Mock the title component
jest.mock("#/components/base/typograpghy/Title", () => {
  return function MockTitle({
    children,
    level,
    className,
  }: {
    children: React.ReactNode;
    level?: number;
    className?: string;
  }) {
    return (
      <h1 data-testid="title" data-level={level} className={className}>
        {children}
      </h1>
    );
  };
});

// Mock the text component
jest.mock("#/components/base/typograpghy/Text", () => {
  return function MockText({
    children,
    className,
  }: {
    children: React.ReactNode;
    className?: string;
  }) {
    return (
      <p data-testid="text" className={className}>
        {children}
      </p>
    );
  };
});

// Mock the flex component
jest.mock("#/components/base/flex/Flex", () => {
  return function MockFlex({
    children,
    vertical,
    align,
    className,
  }: {
    children: React.ReactNode;
    vertical?: boolean;
    align?: string;
    className?: string;
  }) {
    return (
      <div
        data-testid="flex"
        data-vertical={vertical}
        data-align={align}
        className={className}
      >
        {children}
      </div>
    );
  };
});

describe("LandingScreen", () => {
  test("should render welcome title and description", () => {
    render(<LandingScreen />);

    expect(screen.getByText("Welcome to OllamaFlow")).toBeInTheDocument();
    expect(
      screen.getByText(
        "Create and manage your Ollama frontends and backends with our intuitive interface"
      )
    ).toBeInTheDocument();
  });

  test("should render create frontend card", () => {
    render(<LandingScreen />);

    // Use getAllByText and filter to get the title specifically
    const frontendTitles = screen.getAllByText("Create Frontend");
    const frontendTitle = frontendTitles.find(
      (element) => element.tagName === "H1"
    );
    expect(frontendTitle).toBeInTheDocument();

    expect(
      screen.getByText(
        "Create a virtual Ollama server exposed to your network, mapped to your backends and with models that should be automatically deployed across your fleet."
      )
    ).toBeInTheDocument();
  });

  test("should render create backend card", () => {
    render(<LandingScreen />);

    // Use getAllByText and filter to get the title specifically
    const backendTitles = screen.getAllByText("Create Backend");
    const backendTitle = backendTitles.find(
      (element) => element.tagName === "H1"
    );
    expect(backendTitle).toBeInTheDocument();

    expect(
      screen.getByText(
        "Define the backend physical virtual Ollama servers that are workers for your frontend virtual Ollama servers."
      )
    ).toBeInTheDocument();
  });

  test("should render both cards", () => {
    render(<LandingScreen />);

    const cards = screen.getAllByTestId("card");
    expect(cards).toHaveLength(2);
  });

  test("should render buttons with correct links", () => {
    render(<LandingScreen />);

    // Use getAllByText and filter to get the buttons specifically
    const frontendElements = screen.getAllByText("Create Frontend");
    const frontendButton = frontendElements.find(
      (element) => element.tagName === "BUTTON"
    );
    const frontendLink = frontendButton?.closest("a");

    const backendElements = screen.getAllByText("Create Backend");
    const backendButton = backendElements.find(
      (element) => element.tagName === "BUTTON"
    );
    const backendLink = backendButton?.closest("a");

    expect(frontendLink).toHaveAttribute("href", paths.DashboardCreateFrontend);
    expect(backendLink).toHaveAttribute("href", paths.DashboardCreateBackend);
  });

  test("should render buttons with correct properties", () => {
    render(<LandingScreen />);

    const buttons = screen.getAllByTestId("button");
    expect(buttons).toHaveLength(2);

    buttons.forEach((button) => {
      expect(button).toHaveAttribute("data-type", "primary");
      expect(button).toHaveAttribute("data-size", "large");
    });
  });

  test("should not call onNavigate when not provided", () => {
    render(<LandingScreen />);

    const cards = screen.getAllByTestId("card");
    fireEvent.click(cards[0]); // Frontend card
    fireEvent.click(cards[1]); // Backend card

    // Should not throw error
    expect(cards).toHaveLength(2);
  });

  test("should render with correct flex alignment", () => {
    render(<LandingScreen />);

    const flexElements = screen.getAllByTestId("flex");

    // Check for vertical flex elements
    const verticalFlex = flexElements.find(
      (flex) => flex.getAttribute("data-vertical") === "true"
    );
    expect(verticalFlex).toBeInTheDocument();
  });

  test("should render with correct title levels", () => {
    render(<LandingScreen />);

    const titles = screen.getAllByTestId("title");

    // Main title should be level 1
    const mainTitle = titles.find(
      (title) => title.textContent === "Welcome to OllamaFlow"
    );
    expect(mainTitle).toHaveAttribute("data-level", "1");

    // Card titles should be level 2
    const cardTitles = titles.filter(
      (title) =>
        title.textContent === "Create Frontend" ||
        title.textContent === "Create Backend"
    );
    cardTitles.forEach((title) => {
      expect(title).toHaveAttribute("data-level", "2");
    });
  });

  test("should render with correct CSS classes", () => {
    render(<LandingScreen />);

    const cards = screen.getAllByTestId("card");
    cards.forEach((card) => {
      expect(card).toHaveClass("card");
    });
  });

  test("should render with icons", () => {
    render(<LandingScreen />);

    // Check for FileTextOutlined and CloudServerOutlined icons
    const frontendElements = screen.getAllByText("Create Frontend");
    const frontendCard = frontendElements
      .find((element) => element.tagName === "H1")
      ?.closest('[data-testid="card"]');

    const backendElements = screen.getAllByText("Create Backend");
    const backendCard = backendElements
      .find((element) => element.tagName === "H1")
      ?.closest('[data-testid="card"]');

    expect(frontendCard).toBeInTheDocument();
    expect(backendCard).toBeInTheDocument();
  });

  test("should render with responsive column classes", () => {
    render(<LandingScreen />);

    // The Row and Col components should be rendered
    const frontendTitles = screen.getAllByText("Create Frontend");
    const backendTitles = screen.getAllByText("Create Backend");

    expect(frontendTitles.length).toBeGreaterThan(0);
    expect(backendTitles.length).toBeGreaterThan(0);
  });

  test("should render with correct text alignment", () => {
    render(<LandingScreen />);

    const textElements = screen.getAllByTestId("text");

    // Check for text-center class on card descriptions
    const cardDescriptions = textElements.filter(
      (text) =>
        text.textContent?.includes("Create a new frontend configuration") ||
        text.textContent?.includes("Create a new backend configuration")
    );

    cardDescriptions.forEach((text) => {
      expect(text).toHaveClass("text-center");
    });
  });

  test("should render with correct button icons", () => {
    render(<LandingScreen />);

    const buttons = screen.getAllByTestId("button");
    expect(buttons).toHaveLength(2);

    // Check that each button has the correct text content
    const buttonTexts = buttons.map((button) => button.textContent);
    expect(buttonTexts).toContain("Create Frontend");
    expect(buttonTexts).toContain("Create Backend");
  });

  test("should render with proper spacing classes", () => {
    render(<LandingScreen />);

    const flexElements = screen.getAllByTestId("flex");

    // Check for mt-xl class on main flex container
    const mainFlex = flexElements.find((flex) =>
      flex.textContent?.includes("Welcome to OllamaFlow")
    );
    expect(mainFlex).toHaveClass("mt-xl");
  });

  test("should render with proper height classes", () => {
    render(<LandingScreen />);

    const flexElements = screen.getAllByTestId("flex");

    // Check for h-100 class on card flex containers
    const cardFlexElements = flexElements.filter(
      (flex) =>
        flex.textContent?.includes("Create Frontend") ||
        flex.textContent?.includes("Create Backend")
    );

    cardFlexElements.forEach((flex) => {
      expect(flex).toHaveClass("h-100");
    });
  });
});
