import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider, useTheme } from '../theme/ThemeProvider';

function ThemeProbe() {
  const { theme, resolvedTheme, setTheme } = useTheme();
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <span data-testid="resolved">{resolvedTheme}</span>
      <button onClick={() => setTheme('light')}>choose light</button>
      <button onClick={() => setTheme('dark')}>choose dark</button>
      <button onClick={() => setTheme('system')}>choose system</button>
    </div>
  );
}

function renderProbe() {
  return render(
    <ThemeProvider>
      <ThemeProbe />
    </ThemeProvider>,
  );
}

beforeEach(() => {
  localStorage.clear();
  delete document.documentElement.dataset.theme;
});

describe('ThemeProvider', () => {
  it('defaults to the dark theme and applies it to the document', () => {
    renderProbe();

    expect(screen.getByTestId('theme')).toHaveTextContent('dark');
    expect(screen.getByTestId('resolved')).toHaveTextContent('dark');
    expect(document.documentElement.dataset.theme).toBe('dark');
  });

  it('applies the chosen theme to the document and persists it', async () => {
    const user = userEvent.setup();
    renderProbe();

    await user.click(screen.getByRole('button', { name: 'choose light' }));

    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
    expect(document.documentElement.dataset.theme).toBe('light');
    expect(localStorage.getItem('onidash.theme')).toBe('light');
  });

  it('resolves the system theme when matchMedia reports a light preference', async () => {
    const user = userEvent.setup();
    renderProbe();

    await user.click(screen.getByRole('button', { name: 'choose system' }));

    expect(screen.getByTestId('theme')).toHaveTextContent('system');
    // The vitest stub reports prefers-color-scheme: dark as false → light.
    expect(screen.getByTestId('resolved')).toHaveTextContent('light');
    expect(document.documentElement.dataset.theme).toBe('light');
  });

  it('keeps the persisted choice across provider remounts', async () => {
    const user = userEvent.setup();
    const { unmount } = renderProbe();
    await user.click(screen.getByRole('button', { name: 'choose light' }));
    unmount();

    renderProbe();

    expect(screen.getByTestId('theme')).toHaveTextContent('light');
    expect(document.documentElement.dataset.theme).toBe('light');
  });
});
