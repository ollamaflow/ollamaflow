import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import FallBack, { FallBackEnums } from '#/components/base/fallback/FallBack';

describe('FallBack', () => {
  test('should render with default error type', () => {
    render(<FallBack />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  test('should render with custom children', () => {
    const customMessage = 'Custom error message';
    render(<FallBack>{customMessage}</FallBack>);

    expect(screen.getByText(customMessage)).toBeInTheDocument();
  });

  test('should render with error type', () => {
    render(<FallBack type={FallBackEnums.ERROR} />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  test('should render with warning type', () => {
    render(<FallBack type={FallBackEnums.WARNING} />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  test('should render with info type', () => {
    render(<FallBack type={FallBackEnums.INFO} />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  test('should render without retry button when retry is not provided', () => {
    render(<FallBack retry={undefined} />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
    expect(screen.queryByText('Retry')).not.toBeInTheDocument();
  });

  test('should call retry function when retry button is clicked', () => {
    const mockRetry = jest.fn();
    render(<FallBack retry={mockRetry} />);

    const retryButton = screen.getByText('Retry');
    fireEvent.click(retryButton);

    expect(mockRetry).toHaveBeenCalledTimes(1);
  });

  test('should render with custom icon', () => {
    const customIcon = <div data-testid="custom-icon">Custom Icon</div>;
    render(<FallBack icon={customIcon} />);

    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
  });

  test('should render with custom className', () => {
    const customClass = 'custom-fallback-class';
    const { container } = render(<FallBack className={customClass} />);

    expect(container.firstChild).toHaveClass(customClass);
  });

  test('should render with textProps', () => {
    const textProps = { strong: true };
    render(<FallBack textProps={textProps} />);

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  test('should render with all props combined', () => {
    const mockRetry = jest.fn();
    const customIcon = <div data-testid="custom-icon">Custom Icon</div>;
    const customClass = 'custom-fallback-class';
    const textProps = { strong: true };

    render(
      <FallBack
        type={FallBackEnums.WARNING}
        retry={mockRetry}
        icon={customIcon}
        className={customClass}
        textProps={textProps}
      >
        Custom error message
      </FallBack>
    );

    expect(screen.getByText('Custom error message')).toBeInTheDocument();
    expect(screen.getByTestId('custom-icon')).toBeInTheDocument();
    expect(screen.getByText('Retry')).toBeInTheDocument();
  });
});
