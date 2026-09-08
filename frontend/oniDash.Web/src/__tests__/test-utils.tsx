import { render, type RenderOptions } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { ReactElement } from 'react';
import App from '../App';
import { ThemeProvider } from '../theme/ThemeProvider';

/**
 * Renders the full app (shell + routes) inside the providers it needs, at a chosen
 * initial route.
 */
export function renderApp(
  initialRoute = '/',
  options?: Omit<RenderOptions, 'wrapper'>,
): ReturnType<typeof render> {
  return render(
    <ThemeProvider>
      <MemoryRouter initialEntries={[initialRoute]}>
        <App />
      </MemoryRouter>
    </ThemeProvider>,
    options,
  );
}

export function renderWithProviders(ui: ReactElement): ReturnType<typeof render> {
  return render(
    <ThemeProvider>
      <MemoryRouter>{ui}</MemoryRouter>
    </ThemeProvider>,
  );
}
