import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';

const apiMocks = vi.hoisted(() => ({
  fetchLibraries: vi.fn(), fetchMusicArtists: vi.fn(), fetchMusicAlbums: vi.fn(), fetchMusicTracks: vi.fn(),
  fetchMusicOverview: vi.fn(), fetchMusicPlaylists: vi.fn(), fetchMusicFavorites: vi.fn(), fetchMusicHistory: vi.fn(), fetchMusicTopTracks: vi.fn(),
}));
vi.mock('../api/libraries', async (importOriginal) => ({ ...(await importOriginal<typeof import('../api/libraries')>()), fetchLibraries: apiMocks.fetchLibraries }));
vi.mock('../api/music', async (importOriginal) => ({ ...(await importOriginal<typeof import('../api/music')>()), ...apiMocks }));

const musicLibrary = { id: 'lib-1', name: 'Music', createdAtUtc: '2026-01-15T10:00:00Z' };
const album = { id: 'album-1', title: 'OutRun', artistName: 'Kavinsky', year: 2013, hasCover: false };
function track(overrides: Partial<{ id: string; title: string; artistName: string; albumTitle: string; albumId: string }> = {}) {
  const { id = 'track-1', title = 'Nightcall', artistName = 'Kavinsky', albumTitle = 'OutRun', albumId = 'album-1' } = overrides;
  return { id, mediaItemId: `item-${id}`, title, artistName, albumTitle, albumId, hasCover: false, trackNumber: 1, discNumber: null, year: 2013, durationSeconds: 258.4, genre: 'Synthwave', rating: 0 };
}

beforeAll(() => {
  window.HTMLMediaElement.prototype.play = vi.fn().mockResolvedValue(undefined);
  window.HTMLMediaElement.prototype.pause = vi.fn();
  window.HTMLMediaElement.prototype.load = vi.fn();
});

beforeEach(() => {
  vi.resetAllMocks();
  apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
  apiMocks.fetchMusicArtists.mockResolvedValue([{ id: 'artist-1', name: 'Kavinsky' }]);
  apiMocks.fetchMusicAlbums.mockResolvedValue([album]);
  apiMocks.fetchMusicTracks.mockResolvedValue([track()]);
  apiMocks.fetchMusicOverview.mockResolvedValue({ tracks: 1, albums: 1, artists: 1, playlists: 0, favorites: 0, listeningSeconds: 258 });
  apiMocks.fetchMusicPlaylists.mockResolvedValue([]);
  apiMocks.fetchMusicFavorites.mockResolvedValue([]);
  apiMocks.fetchMusicHistory.mockResolvedValue([{ id: 'history-1', trackId: 'track-1', startedAtUtc: '2026-09-13T10:00:00Z', completedAtUtc: null, playedSeconds: 82, completionRatio: 0.31, source: 'local' }]);
  apiMocks.fetchMusicTopTracks.mockResolvedValue([{ trackId: 'track-1', plays: 12, playedSeconds: 1800 }]);
});

describe('MusicDashboardPage phase one', () => {
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
    expect(await screen.findByText('Nightcall')).toBeInTheDocument();
    expect(screen.getAllByText('Kavinsky').length).toBeGreaterThan(0);
    expect(screen.getByText(/Synthwave/)).toBeInTheDocument();
    expect(screen.getByText('Recently played')).toBeInTheDocument();
    expect(screen.getByText('Most played')).toBeInTheDocument();
    expect(screen.getByText('Albums')).toBeInTheDocument();
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
