import { screen, waitFor, within } from '@testing-library/react';
import { renderApp } from './test-utils';

const apiMocks = vi.hoisted(() => ({
  fetchContinueMedia: vi.fn(),
  fetchRecentlyAdded: vi.fn(),
  fetchRecentlyPlayed: vi.fn(),
  fetchFavorites: vi.fn(),
  fetchActivityTimeline: vi.fn(),
  fetchLocalRecommendations: vi.fn(),
}));
vi.mock('../api/dashboard', async (importOriginal) => ({ ...(await importOriginal<typeof import('../api/dashboard')>()), ...apiMocks }));

const mediaItem = (id: string, title: string, mediaType: string) => ({
  id, title, mediaType, extension: '.flac', hasArtwork: false, timestamp: '2026-09-10T09:30:00Z', isFavorite: false,
});
const activityEntry = {
  id: 'act-1', mediaItemId: 'item-1', title: 'Dune', mediaType: 'Video', action: 'Updated media item', occurredAtUtc: '2026-09-10T09:30:00Z',
};

describe('UnifiedDashboardPage', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    apiMocks.fetchContinueMedia.mockResolvedValue([mediaItem('m-1', 'Nightcall', 'Audio')]);
    apiMocks.fetchRecentlyAdded.mockResolvedValue([mediaItem('m-2', 'Berserk Chapter 360', 'Manga')]);
    apiMocks.fetchRecentlyPlayed.mockResolvedValue([]);
    apiMocks.fetchFavorites.mockResolvedValue([mediaItem('m-3', 'Interstellar', 'Video')]);
    apiMocks.fetchActivityTimeline.mockResolvedValue([activityEntry]);
    apiMocks.fetchLocalRecommendations.mockResolvedValue([]);
  });

  it('loads every unified rail through the API client', async () => {
    renderApp('/unified');
    expect(await screen.findByRole('heading', { name: 'Your media dashboard' })).toBeInTheDocument();
    await waitFor(() => {
      expect(apiMocks.fetchContinueMedia).toHaveBeenCalledWith(6, expect.any(AbortSignal));
      expect(apiMocks.fetchRecentlyAdded).toHaveBeenCalledWith(6, expect.any(AbortSignal));
      expect(apiMocks.fetchRecentlyPlayed).toHaveBeenCalledWith(6, expect.any(AbortSignal));
      expect(apiMocks.fetchFavorites).toHaveBeenCalledWith(6, expect.any(AbortSignal));
      expect(apiMocks.fetchActivityTimeline).toHaveBeenCalledWith(10, expect.any(AbortSignal));
      expect(apiMocks.fetchLocalRecommendations).toHaveBeenCalledWith(6, expect.any(AbortSignal));
    });
  });

  it('renders cross-catalogue cards and the activity timeline', async () => {
    renderApp('/unified');

    const continueSection = await screen.findByRole('heading', { name: 'Continue Reading / Watching / Listening' });
    const card = (continueSection.closest('section') as HTMLElement);
    expect(within(card).getByText('Nightcall')).toBeInTheDocument();
    expect(within(card).getByText('Audio')).toBeInTheDocument();

    const favorites = within(screen.getByRole('heading', { name: 'Favorites Across Media' }).closest('section') as HTMLElement);
    expect(await favorites.findByText('Interstellar')).toBeInTheDocument();

    const timeline = within(screen.getByRole('heading', { name: 'Activity Timeline' }).closest('section') as HTMLElement);
    expect(await timeline.findByText('Dune')).toBeInTheDocument();
    expect(timeline.getByText('Updated media item')).toBeInTheDocument();
  });

  it('shows the empty state for a rail with no local data', async () => {
    renderApp('/unified');
    const recent = await screen.findByText('No recent playback activity.');
    expect(recent).toBeInTheDocument();
  });

  it('surfaces an error state when the dashboard API fails', async () => {
    apiMocks.fetchContinueMedia.mockRejectedValue(new Error('offline'));
    renderApp('/unified');
    expect(await screen.findByText('Dashboard error')).toBeInTheDocument();
    expect(screen.getByText('offline')).toBeInTheDocument();
  });
});
