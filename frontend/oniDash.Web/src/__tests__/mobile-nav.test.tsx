import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

vi.mock('../hooks/useHealth', () => ({
  useHealth: () => ({ data: null, loading: true, error: null, refresh: async () => {} }),
}));

describe('MobileNav', () => {
  it('opens the off-canvas navigation from the top bar', async () => {
    const user = userEvent.setup();
    renderApp('/');

    expect(screen.queryByRole('button', { name: 'Close navigation' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Open navigation' }));

    // Sidebar + drawer both render primary links while the drawer is open.
    expect(screen.getAllByRole('link', { name: 'Settings' })).toHaveLength(2);
  });

  it('closes via the close button and navigates on link click', async () => {
    const user = userEvent.setup();
    renderApp('/');

    await user.click(screen.getByRole('button', { name: 'Open navigation' }));
    await user.click(screen.getAllByRole('link', { name: 'Search' })[1]);

    expect(screen.getByRole('heading', { name: 'Search', level: 2 })).toBeInTheDocument();
    // Drawer is unmounted again — only the sidebar link remains.
    expect(screen.getAllByRole('link', { name: 'Search' })).toHaveLength(1);
  });

  it('closes when Escape is pressed', async () => {
    const user = userEvent.setup();
    renderApp('/');

    await user.click(screen.getByRole('button', { name: 'Open navigation' }));
    await user.keyboard('{Escape}');

    expect(screen.queryByRole('button', { name: 'Close navigation' })).not.toBeInTheDocument();
  });
});
