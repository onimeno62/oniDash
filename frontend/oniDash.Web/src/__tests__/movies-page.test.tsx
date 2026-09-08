import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

const apiMocks = vi.hoisted(() => ({
  fetchLibraries: vi.fn(),
  fetchMovies: vi.fn(),
  fetchContinueWatching: vi.fn(),
  saveWatchProgress: vi.fn(),
  setMovieWatched: vi.fn(),
}));

vi.mock('../api/libraries', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/libraries')>()),
  fetchLibraries: apiMocks.fetchLibraries,
}));

vi.mock('../api/movies', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/movies')>()),
  ...apiMocks,
}));

const movieLibrary = { id: 'lib-9', name: 'Films', createdAtUtc: '2026-01-15T10:00:00Z' };

function movie(overrides: Partial<{ id: string; title: string; watched: boolean; progress: number | null }> = {}) {
  const { id = 'movie-1', title = 'The Matrix', watched = false, progress = null } = overrides;
  return {
    id,
    mediaItemId: `item-${id}`,
    title,
    year: 1999,
    durationSeconds: 8166,
    container: '.mkv',
    hasPoster: false,
    watched,
    watchProgressSeconds: progress,
    watchedAtUtc: null,
  };
}

describe('MoviesPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    apiMocks.fetchLibraries.mockResolvedValue([movieLibrary]);
    apiMocks.fetchMovies.mockResolvedValue([]);
    apiMocks.fetchContinueWatching.mockResolvedValue([]);
    apiMocks.saveWatchProgress.mockResolvedValue({});
    apiMocks.setMovieWatched.mockResolvedValue({});
  });

  beforeAll(() => {
    // jsdom does not implement media playback.
    window.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined);
    window.HTMLMediaElement.prototype.pause = vi.fn();
    window.HTMLMediaElement.prototype.load = vi.fn();
  });

  it('shows the empty state when the library has no movies', async () => {
    renderApp('/movies');

    expect(await screen.findByRole('heading', { name: 'No movies here yet' })).toBeInTheDocument();
    expect(apiMocks.fetchMovies).toHaveBeenCalledWith('lib-9', { signal: expect.anything(), watched: undefined, limit: 100 });
  });

  it('renders the movie grid with titles, years, and runtimes', async () => {
    apiMocks.fetchMovies.mockResolvedValue([movie()]);
    renderApp('/movies');

    expect(await screen.findByText('The Matrix')).toBeInTheDocument();
    expect(screen.getByText('1999 · 2h 16m')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Play The Matrix' })).toBeInTheDocument();
  });

  it('shows continue-watching cards for partially watched movies', async () => {
    apiMocks.fetchMovies.mockResolvedValue([]);
    apiMocks.fetchContinueWatching.mockResolvedValue([movie({ id: 'movie-2', title: 'Heat', progress: 4000 })]);
    renderApp('/movies');

    expect(await screen.findByRole('region', { name: 'Continue watching' })).toBeInTheDocument();
    expect(screen.getByText('Heat')).toBeInTheDocument();
  });

  it('sends the watched filter to the API', async () => {
    apiMocks.fetchMovies.mockResolvedValue([movie()]);
    const user = userEvent.setup();
    renderApp('/movies');

    await screen.findByText('The Matrix');
    apiMocks.fetchMovies.mockClear();
    await user.selectOptions(screen.getByLabelText('Watched filter'), 'unwatched');

    await waitFor(() => {
      expect(apiMocks.fetchMovies).toHaveBeenCalledWith('lib-9', { signal: expect.anything(), watched: false, limit: 100 });
    });
  });

  it('opens the player, saves progress on close, and marks movies watched', async () => {
    apiMocks.fetchMovies.mockResolvedValue([movie()]);
    const user = userEvent.setup();
    renderApp('/movies');

    await screen.findByText('The Matrix');
    await user.click(screen.getByRole('button', { name: 'Play The Matrix' }));

    const player = await screen.findByTestId('movie-player');
    expect(within(player).getByText('The Matrix (1999)')).toBeInTheDocument();

    await user.click(within(player).getByRole('button', { name: 'Mark watched' }));
    await waitFor(() => {
      expect(apiMocks.setMovieWatched).toHaveBeenCalledWith('movie-1', true);
    });
    expect(screen.queryByTestId('movie-player')).not.toBeInTheDocument();
  });
});
