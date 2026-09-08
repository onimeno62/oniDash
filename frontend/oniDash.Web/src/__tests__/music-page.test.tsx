import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

const apiMocks = vi.hoisted(() => ({
  fetchLibraries: vi.fn(),
  fetchMusicArtists: vi.fn(),
  fetchMusicAlbums: vi.fn(),
  fetchMusicTracks: vi.fn(),
}));

vi.mock('../api/libraries', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/libraries')>()),
  fetchLibraries: apiMocks.fetchLibraries,
}));

vi.mock('../api/music', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/music')>()),
  fetchMusicArtists: apiMocks.fetchMusicArtists,
  fetchMusicAlbums: apiMocks.fetchMusicAlbums,
  fetchMusicTracks: apiMocks.fetchMusicTracks,
}));

const musicLibrary = { id: 'lib-1', name: 'Music', createdAtUtc: '2026-01-15T10:00:00Z' };

// jsdom does not implement media playback; the player only needs the control surface.
beforeAll(() => {
  window.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined);
  window.HTMLMediaElement.prototype.pause = vi.fn();
});

const artist = { id: 'artist-1', name: 'Kavinsky' };

const album = {
  id: 'album-1',
  title: 'OutRun',
  artistName: 'Kavinsky',
  year: 2013,
  hasCover: false,
};

const track = {
  id: 'track-1',
  mediaItemId: 'item-1',
  title: 'Nightcall',
  artistName: 'Kavinsky',
  albumTitle: 'OutRun',
  albumId: 'album-1',
  hasCover: false,
  trackNumber: 1,
  discNumber: null,
  year: 2013,
  durationSeconds: 258.4,
  genre: 'Synthwave',
};

describe('MusicPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchMusicArtists.mockResolvedValue([]);
    apiMocks.fetchMusicAlbums.mockResolvedValue([]);
    apiMocks.fetchMusicTracks.mockResolvedValue([]);
  });

  async function seedCatalog() {
    apiMocks.fetchMusicArtists.mockResolvedValue([artist]);
    apiMocks.fetchMusicAlbums.mockResolvedValue([album]);
    apiMocks.fetchMusicTracks.mockResolvedValue([track]);
  }

  it('shows the empty state when the library has no tagged music', async () => {
    apiMocks.fetchMusicArtists.mockResolvedValue([]);
    renderApp('/music');

    expect(await screen.findByRole('heading', { name: 'No tagged music yet' })).toBeInTheDocument();
    expect(apiMocks.fetchMusicArtists).toHaveBeenCalledWith('lib-1', expect.anything());
  });

  it('renders artists, album cards, and track rows from the catalogue', async () => {
    await seedCatalog();
    renderApp('/music');

    expect(await screen.findByRole('button', { name: 'Kavinsky' })).toBeInTheDocument();
    const albumsSection = screen.getByRole('region', { name: 'Albums' });
    expect(within(albumsSection).getByText('OutRun')).toBeInTheDocument();
    const tracksSection = screen.getByRole('region', { name: 'Tracks' });
    expect(within(tracksSection).getByText('Nightcall')).toBeInTheDocument();
    expect(within(tracksSection).getByText('4:18')).toBeInTheDocument();
  });

  it('plays a track through the player bar when its play button is clicked', async () => {
    await seedCatalog();
    const user = userEvent.setup();
    renderApp('/music');

    await screen.findByRole('button', { name: /Play Nightcall/ });
    await user.click(screen.getByRole('button', { name: /Play Nightcall/ }));

    const bar = await screen.findByTestId('player-bar');
    expect(within(bar).getByText('Nightcall')).toBeInTheDocument();
    expect(within(bar).getByRole('button', { name: 'Pause' })).toBeInTheDocument();

    await user.click(within(bar).getByRole('button', { name: 'Stop' }));
    await waitFor(() => {
      expect(screen.queryByTestId('player-bar')).not.toBeInTheDocument();
    });
  });

  it('filters albums and tracks when an artist chip is selected', async () => {
    await seedCatalog();
    const user = userEvent.setup();
    renderApp('/music');

    await screen.findByRole('button', { name: 'Kavinsky' });
    apiMocks.fetchMusicAlbums.mockClear();
    apiMocks.fetchMusicTracks.mockClear();
    await user.click(screen.getByRole('button', { name: 'Kavinsky' }));

    await waitFor(() => {
      expect(apiMocks.fetchMusicAlbums).toHaveBeenCalledWith('lib-1', {
        artistId: 'artist-1',
        signal: expect.anything(),
      });
    });
    expect(apiMocks.fetchMusicTracks).toHaveBeenCalledWith('lib-1', {
      artistId: 'artist-1',
      signal: expect.anything(),
    });
  });

  it('shows an error state with retry when the catalogue request fails', async () => {
    apiMocks.fetchMusicArtists.mockRejectedValue(new Error('offline'));
    const user = userEvent.setup();
    renderApp('/music');

    expect(await screen.findByRole('alert')).toHaveTextContent('Music catalogue unavailable');
    apiMocks.fetchMusicArtists.mockResolvedValue([]);
    await user.click(screen.getByRole('button', { name: /Retry/ }));
    expect(await screen.findByRole('heading', { name: 'No tagged music yet' })).toBeInTheDocument();
  });
});
