import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

const apiMocks = vi.hoisted(() => ({
  fetchLibraries: vi.fn(), fetchMusicArtists: vi.fn(), fetchMusicAlbums: vi.fn(), fetchMusicTracks: vi.fn(),
  fetchMusicOverview: vi.fn(), fetchMusicPlaylists: vi.fn(), fetchMusicGenres: vi.fn(), fetchMusicFavorites: vi.fn(), fetchMusicHistory: vi.fn(), fetchMusicTopTracks: vi.fn(),
}));
vi.mock('../api/libraries', async (importOriginal) => ({ ...(await importOriginal<typeof import('../api/libraries')>()), fetchLibraries: apiMocks.fetchLibraries }));
vi.mock('../api/music', async (importOriginal) => ({ ...(await importOriginal<typeof import('../api/music')>()), ...apiMocks }));

const musicLibrary = { id: 'lib-1', name: 'Music', createdAtUtc: '2026-01-15T10:00:00Z' };
// Media playback is stubbed globally in vitest.setup.ts. Do not re-stub it with vi.fn()
// here: the beforeEach vi.resetAllMocks() below neuters such mocks, so play() would return
// undefined and the player's `play().catch(...)` chain would throw an unhandled TypeError.
const artist = { id: 'artist-1', name: 'Kavinsky' };
const album = { id: 'album-1', title: 'OutRun', artistName: 'Kavinsky', year: 2013, hasCover: false };
function track(overrides: Partial<{ id: string; title: string; artistName: string; albumTitle: string; albumId: string }> = {}) {
  const { id = 'track-1', title = 'Nightcall', artistName = 'Kavinsky', albumTitle = 'OutRun', albumId = 'album-1' } = overrides;
  return { id, mediaItemId: `item-${id}`, title, artistName, albumTitle, albumId, hasCover: false, trackNumber: 1, discNumber: null, year: 2013, durationSeconds: 258.4, genre: 'Synthwave', rating: 0 };
}

function resetApiMocks() {
  vi.resetAllMocks();
  apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
  apiMocks.fetchMusicArtists.mockResolvedValue([]);
  apiMocks.fetchMusicAlbums.mockResolvedValue([]);
  apiMocks.fetchMusicTracks.mockResolvedValue([]);
  apiMocks.fetchMusicOverview.mockResolvedValue(null);
  apiMocks.fetchMusicPlaylists.mockResolvedValue([]);
  apiMocks.fetchMusicGenres.mockResolvedValue([]);
  apiMocks.fetchMusicFavorites.mockResolvedValue([]);
  apiMocks.fetchMusicHistory.mockResolvedValue([]);
  apiMocks.fetchMusicTopTracks.mockResolvedValue([]);
}

describe('MusicDashboardPage phase one', () => {
  beforeEach(() => {
    resetApiMocks();
    apiMocks.fetchMusicArtists.mockResolvedValue([artist]);
    apiMocks.fetchMusicAlbums.mockResolvedValue([album]);
    apiMocks.fetchMusicTracks.mockResolvedValue([track()]);
    apiMocks.fetchMusicOverview.mockResolvedValue({ tracks: 1, albums: 1, artists: 1, playlists: 0, favorites: 0, listeningSeconds: 258 });
    apiMocks.fetchMusicHistory.mockResolvedValue([{ id: 'history-1', trackId: 'track-1', startedAtUtc: '2026-09-13T10:00:00Z', completedAtUtc: null, playedSeconds: 82, completionRatio: 0.31, source: 'local' }]);
    apiMocks.fetchMusicTopTracks.mockResolvedValue([{ trackId: 'track-1', plays: 12, playedSeconds: 1800 }]);
  });

  it('renders the new continue-listening dashboard without a music sidebar', async () => {
    renderApp('/music');
    expect(await screen.findByRole('heading', { name: 'Your library' })).toBeInTheDocument();
    expect(screen.getByText('Continue listening')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Standard' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Bento' })).toBeInTheDocument();
    expect(screen.queryByText('MUSIC STUDIO')).not.toBeInTheDocument();
  });

  it('shows real playback and library metadata in the hero and carousels', async () => {
    renderApp('/music');
    // The hero and the carousels both show the seeded track, so the title is expected
    // to appear more than once. Assert presence rather than uniqueness.
    expect((await screen.findAllByText('Nightcall')).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Kavinsky').length).toBeGreaterThan(0);
    expect(screen.getByText(/Synthwave/)).toBeInTheDocument();
    // Section headers and the stats grid share labels like "Albums"/"Artists", so these
    // are presence checks rather than uniqueness checks.
    expect(screen.getByText('Recently played')).toBeInTheDocument();
    expect(screen.getByText('Most played')).toBeInTheDocument();
    expect(screen.getAllByText('Albums').length).toBeGreaterThan(0);
  });

  it('switches to and persists the Bento dashboard layout', async () => {
    const user = userEvent.setup();
    renderApp('/music');
    await screen.findByRole('heading', { name: 'Your library' });
    await user.click(screen.getByRole('button', { name: 'Bento' }));
    expect(screen.getByRole('button', { name: 'Bento' })).toHaveClass('bg-primary');
    expect(localStorage.getItem('onidash.music.dashboard.layout')).toBe('bento');
  });
});

describe('MusicWorkspacePage (detailed music command centre)', () => {
  beforeEach(resetApiMocks);
  async function seedCatalog() { apiMocks.fetchMusicArtists.mockResolvedValue([artist]); apiMocks.fetchMusicAlbums.mockResolvedValue([album]); apiMocks.fetchMusicTracks.mockResolvedValue([track()]); }
  it('loads the workspace for the first library and queries the catalogue', async () => { renderApp('/music/workspace'); expect(await screen.findByRole('heading', { name: 'Your music universe.' })).toBeInTheDocument(); expect(apiMocks.fetchMusicArtists).toHaveBeenCalledWith('lib-1', { limit: 500 }); expect(apiMocks.fetchMusicTracks).toHaveBeenCalledWith('lib-1', { limit: 1000 }); });
  it('renders albums and track rows from the catalogue in the library tab', async () => { await seedCatalog(); const user = userEvent.setup(); renderApp('/music/workspace'); await screen.findByRole('heading', { name: 'Your music universe.' }); await user.click(screen.getByRole('button', { name: /All songs/ })); expect(screen.getByText('OutRun')).toBeInTheDocument(); expect(screen.getByText('Nightcall')).toBeInTheDocument(); expect(screen.getByText('4:18')).toBeInTheDocument(); });
  it('plays a track through the player bar when its row is clicked', async () => { await seedCatalog(); const user = userEvent.setup(); renderApp('/music/workspace'); await screen.findByRole('heading', { name: 'Your music universe.' }); await user.click(screen.getByRole('button', { name: /All songs/ })); const rowButtons = await screen.findAllByRole('button', { name: /Nightcall/ }); await user.click(rowButtons[0]); const bar = await screen.findByTestId('player-bar'); expect(within(bar).getAllByText('Nightcall').length).toBeGreaterThan(0); expect(screen.getByRole('button', { name: /^Pause/ })).toBeInTheDocument(); await user.click(within(bar).getByRole('button', { name: 'Stop' })); await waitFor(() => { expect(screen.queryByTestId('player-bar')).not.toBeInTheDocument(); }); });
  it('filters the track list when an artist is selected', async () => { apiMocks.fetchMusicArtists.mockResolvedValue([artist, { id: 'artist-2', name: 'Perturbator' }]); apiMocks.fetchMusicTracks.mockResolvedValue([track(), track({ id: 'track-2', title: 'Cryptonight', artistName: 'Perturbator', albumTitle: 'Uncanny Valley', albumId: 'album-2' })]); const user = userEvent.setup(); renderApp('/music/workspace'); await screen.findByRole('heading', { name: 'Your music universe.' }); await user.click(screen.getByRole('button', { name: /All songs/ })); expect(screen.getByText('Nightcall')).toBeInTheDocument(); expect(screen.getByText('Cryptonight')).toBeInTheDocument(); await user.selectOptions(screen.getByDisplayValue('All artists'), 'Kavinsky'); expect(screen.getByText('Nightcall')).toBeInTheDocument(); expect(screen.queryByText('Cryptonight')).not.toBeInTheDocument(); });
  it('shows an error state with retry when the catalogue request fails', async () => { apiMocks.fetchMusicArtists.mockRejectedValue(new Error('offline')); const user = userEvent.setup(); renderApp('/music/workspace'); expect(await screen.findByRole('alert')).toHaveTextContent('Music dashboard unavailable'); apiMocks.fetchMusicArtists.mockResolvedValue([]); await user.click(screen.getByRole('button', { name: /Retry/ })); expect(await screen.findByRole('heading', { name: 'Your music universe.' })).toBeInTheDocument(); });
});
