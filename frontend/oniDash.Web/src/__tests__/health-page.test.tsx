import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from './test-utils';
import { HealthPage } from '../pages/HealthPage';

const REPORT = {
  status: 'Healthy',
  version: '0.1.0',
  databaseStatus: 'Ok',
  timestamp: '2026-01-01T12:00:00.000Z',
};

function stubFetch() {
  const fetchMock = vi.fn();
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('HealthPage', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows a loading skeleton, then the live report from the API', async () => {
    const fetchMock = stubFetch();
    fetchMock.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => REPORT,
    });

    renderWithProviders(<HealthPage />);

    expect(screen.getByRole('status')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('0.1.0')).toBeInTheDocument();
    });
    expect(screen.getByText('Healthy')).toBeInTheDocument();
    expect(screen.getByText('Ok')).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/health',
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    );
  });

  it('shows an error state with retry when the API is unreachable', async () => {
    const user = userEvent.setup();
    const fetchMock = stubFetch();
    fetchMock
      .mockRejectedValueOnce(new Error('The oniDash API is unreachable.'))
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => REPORT,
      });

    renderWithProviders(<HealthPage />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('API unreachable');

    await user.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => {
      expect(screen.getByText('Healthy')).toBeInTheDocument();
    });
  });

  it('warns about a degraded API when the database is unavailable', async () => {
    stubFetch().mockResolvedValueOnce({
      ok: false,
      status: 503,
      json: async () => ({ ...REPORT, status: 'Unhealthy', databaseStatus: 'Unavailable' }),
    });

    renderWithProviders(<HealthPage />);

    await waitFor(() => {
      expect(screen.getByText('Unhealthy')).toBeInTheDocument();
    });
    expect(screen.getByText('The API is up, but degraded')).toBeInTheDocument();
  });
});
