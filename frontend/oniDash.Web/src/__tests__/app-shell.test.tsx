import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

vi.mock('../hooks/useHealth', () => ({
  useHealth: () => ({ data: null, loading: true, error: null, refresh: async () => {} }),
}));

vi.mock('../hooks/useLibraries', () => ({
  useLibraries: () => ({
    libraries: [],
    loading: false,
    error: null,
    reload: async () => {},
    create: async () => {},
    rename: async () => {},
    remove: async () => {},
  }),
  useLibrarySources: () => ({
    sources: [],
    loading: false,
    error: null,
    reload: async () => {},
    add: async () => {},
    remove: async () => {},
  }),
}));

describe('AppShell', () => {
  it('renders the dashboard with primary navigation', () => {
    renderApp('/');

    expect(screen.getByRole('heading', { name: 'Welcome to oniDash' })).toBeInTheDocument();
    for (const label of ['Dashboard', 'Library', 'Search', 'API Health', 'Settings']) {
      expect(screen.getAllByRole('link', { name: label }).length).toBeGreaterThan(0);
    }
  });

  it('navigates to the library page from the sidebar', async () => {
    const user = userEvent.setup();
    renderApp('/');

    await user.click(screen.getAllByRole('link', { name: 'Library' })[0]);

    expect(screen.getByRole('heading', { name: 'Library', level: 2 })).toBeInTheDocument();
    expect(screen.getByLabelText('Library name')).toBeInTheDocument();
  });

  it('navigates to settings and offers the theme choice', async () => {
    const user = userEvent.setup();
    renderApp('/');

    await user.click(screen.getAllByRole('link', { name: 'Settings' })[0]);

    expect(screen.getByRole('group', { name: 'Theme' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Dark' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('shows a not-found state for unknown routes', () => {
    renderApp('/definitely-not-a-page');

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument();
  });
});
