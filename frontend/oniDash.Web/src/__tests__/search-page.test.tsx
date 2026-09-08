import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

const apiMocks = vi.hoisted(() => ({
  searchMedia: vi.fn(),
  fetchLibraries: vi.fn(),
}));

vi.mock('../api/search', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/search')>()),
  ...apiMocks,
}));

vi.mock('../api/libraries', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/libraries')>()),
  fetchLibraries: apiMocks.fetchLibraries,
}));

import { searchMedia } from '../api/search';

const libraries = [
  { id: 'lib-1', name: 'Music', createdAtUtc: '2026-01-15T10:00:00Z' },
  { id: 'lib-2', name: 'Movies', createdAtUtc: '2026-02-01T10:00:00Z' },
];

const hits = [
  { itemId: 'item-1', displayName: 'Night Drive', libraryId: 'lib-1', libraryName: 'Music' },
  { itemId: 'item-2', displayName: 'Night of the Hunter', libraryId: 'lib-2', libraryName: 'Movies' },
];

describe('SearchPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    apiMocks.fetchLibraries.mockResolvedValue(libraries);
  });

  it('explains the search before any query is typed', () => {
    renderApp('/search');

    expect(
      screen.getByRole('heading', { name: 'Search', level: 2 }),
    ).toBeInTheDocument();
    expect(screen.getByText('Type to search your library')).toBeInTheDocument();
  });

  it('searches as you type and renders ranked results with library names', async () => {
    const user = userEvent.setup();
    apiMocks.searchMedia.mockResolvedValue(hits);

    renderApp('/search');
    await screen.findByRole('combobox', { name: 'Filter by library' });

    await user.type(screen.getByRole('searchbox', { name: 'Search query' }), 'night');

    await screen.findByText('Night Drive');
    expect(screen.getByText('Night of the Hunter')).toBeInTheDocument();
    // The library badge lives in the results list (the filter <select> also says "Music").
    expect(screen.getByTestId('result-count')).toHaveTextContent('2 results');
    await waitFor(() => {
      expect(searchMedia).toHaveBeenCalledWith('night', expect.objectContaining({ limit: 100 }));
    });
  });

  it('shows a no-results state when nothing matches', async () => {
    const user = userEvent.setup();
    apiMocks.searchMedia.mockResolvedValue([]);

    renderApp('/search');
    await user.type(screen.getByRole('searchbox', { name: 'Search query' }), 'zzz');

    expect(await screen.findByText('No results')).toBeInTheDocument();
  });

  it('filters by library when one is selected', async () => {
    const user = userEvent.setup();
    apiMocks.searchMedia.mockResolvedValue([hits[1]]);

    renderApp('/search');
    await user.selectOptions(
      await screen.findByRole('combobox', { name: 'Filter by library' }),
      'lib-2',
    );
    await user.type(screen.getByRole('searchbox', { name: 'Search query' }), 'night');

    await screen.findByText('Night of the Hunter');
    await waitFor(() => {
      expect(searchMedia).toHaveBeenCalledWith('night', expect.objectContaining({ libraryId: 'lib-2' }));
    });
  });

  it('shows a retryable error state when the search API fails', async () => {
    const user = userEvent.setup();
    apiMocks.searchMedia.mockRejectedValue(new Error('The oniDash API is unreachable.'));

    renderApp('/search');
    await user.type(screen.getByRole('searchbox', { name: 'Search query' }), 'night');

    expect(await screen.findByText('Search failed')).toBeInTheDocument();
    expect(screen.getByText('The oniDash API is unreachable.')).toBeInTheDocument();
  });
});
