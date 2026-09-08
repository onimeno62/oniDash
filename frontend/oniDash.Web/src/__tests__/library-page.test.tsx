import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderApp } from './test-utils';
import { ApiError } from '../api/client';

const apiMocks = vi.hoisted(() => ({
  fetchLibraries: vi.fn(),
  createLibrary: vi.fn(),
  renameLibrary: vi.fn(),
  deleteLibrary: vi.fn(),
  fetchSources: vi.fn(),
  addSource: vi.fn(),
  removeSource: vi.fn(),
  fetchLibraryItems: vi.fn(),
  startScan: vi.fn(),
  fetchScan: vi.fn(),
  cancelScan: vi.fn(),
}));

vi.mock('../api/libraries', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/libraries')>()),
  ...apiMocks,
}));

vi.mock('../api/scans', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/scans')>()),
  ...apiMocks,
}));

import { createLibrary, deleteLibrary, addSource, renameLibrary } from '../api/libraries';
import { startScan, cancelScan, type ScanJobSnapshot } from '../api/scans';

const musicLibrary = {
  id: 'lib-1',
  name: 'Music',
  createdAtUtc: '2026-01-15T10:00:00Z',
};

const moviesLibrary = {
  id: 'lib-2',
  name: 'Movies',
  createdAtUtc: '2026-02-01T10:00:00Z',
};

const mainSource = {
  id: 'src-1',
  libraryId: 'lib-1',
  name: 'Main',
  rootPath: 'D:\\Media\\Music',
  createdAtUtc: '2026-01-15T10:00:00Z',
};

function scanSnapshot(overrides: Partial<ScanJobSnapshot> = {}): ScanJobSnapshot {
  return {
    scanId: 'scan-1',
    libraryId: 'lib-1',
    sourceId: 'src-1',
    sourceName: 'Main',
    status: 'Running',
    phase: 'Discovering',
    filesDiscovered: 0,
    filesProcessed: 0,
    filesIndexed: 0,
    filesUpdated: 0,
    filesUnchanged: 0,
    filesMarkedMissing: 0,
    startedAtUtc: '2026-09-07T12:00:00Z',
    completedAtUtc: null,
    error: null,
    ...overrides,
  };
}

describe('LibraryPage', () => {
  beforeEach(() => {
    // resetAllMocks (not clearAllMocks) so queued mockResolvedValueOnce responses
    // from one test can never leak into the next test's initial load.
    vi.resetAllMocks();
    apiMocks.fetchLibraries.mockResolvedValue([]);
    apiMocks.fetchSources.mockResolvedValue([]);
    apiMocks.fetchLibraryItems.mockResolvedValue({ items: [], page: 1, pageSize: 50, totalCount: 0 });
  });

  it('offers a create form when no libraries exist and creates one', async () => {
    const user = userEvent.setup();
    apiMocks.createLibrary.mockResolvedValue(musicLibrary);
    // After creation the page reloads the list; return the new library then.
    apiMocks.fetchLibraries
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([musicLibrary]);

    renderApp('/library');

    const nameInput = await screen.findByLabelText('Library name');
    await user.type(nameInput, 'Music');
    await user.click(screen.getByRole('button', { name: 'Create library' }));

    await waitFor(() => {
      expect(createLibrary).toHaveBeenCalledWith('Music');
    });
    // Scope to main: the sidebar also shows a "Music" nav link.
    const main = screen.getByRole('main');
    await waitFor(() => {
      expect(within(main).getByText('Music')).toBeInTheDocument();
    });
  });

  it('lists existing libraries with rename and delete controls', async () => {
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary, moviesLibrary]);

    renderApp('/library');

    // Scope to main: the sidebar also shows "Music"/"Movies" nav links.
    const main = screen.getByRole('main');
    expect(await within(main).findByText('Music')).toBeInTheDocument();
    expect(await within(main).findByText('Movies')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Rename library Music' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete library Music' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'New library' })).toBeInTheDocument();
  });

  it('renames a library after editing the name inline', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.renameLibrary.mockResolvedValue({ ...musicLibrary, name: 'Anime' });

    renderApp('/library');

    await user.click(await screen.findByRole('button', { name: 'Rename library Music' }));
    const input = screen.getByLabelText('Library name');
    await user.clear(input);
    await user.type(input, 'Anime');
    await user.click(screen.getByRole('button', { name: 'Save library name' }));

    await waitFor(() => {
      expect(renameLibrary).toHaveBeenCalledWith('lib-1', 'Anime');
    });
  });

  it('deletes a library only after explicit confirmation', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries
      .mockResolvedValueOnce([musicLibrary])
      .mockResolvedValue([]);
    apiMocks.deleteLibrary.mockResolvedValue(undefined);

    renderApp('/library');

    await user.click(await screen.findByRole('button', { name: 'Delete library Music' }));

    // First click only asks for confirmation — no delete call yet.
    expect(deleteLibrary).not.toHaveBeenCalled();
    await user.click(screen.getByRole('button', { name: 'Delete' }));

    await waitFor(() => {
      expect(deleteLibrary).toHaveBeenCalledWith('lib-1');
    });
    // Scope to main: the sidebar also shows a "Music" nav link.
    const main = screen.getByRole('main');
    await waitFor(() => {
      expect(within(main).queryByText('Music')).not.toBeInTheDocument();
    });
  });

  it('expands folder sources and adds one for a library', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchSources.mockResolvedValue([
      mainSource,
    ]);
    apiMocks.addSource.mockResolvedValue({
      id: 'src-2',
      libraryId: 'lib-1',
      name: 'Second',
      rootPath: 'D:\\Media\\More',
      createdAtUtc: '2026-01-16T10:00:00Z',
    });

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));

    expect(await screen.findByText('D:\\Media\\Music')).toBeInTheDocument();

    await user.type(screen.getByLabelText('Source name'), 'Second');
    await user.type(screen.getByLabelText('Folder path'), 'D:\\Media\\More');
    await user.click(screen.getByRole('button', { name: 'Add folder' }));

    await waitFor(() => {
      expect(addSource).toHaveBeenCalledWith('lib-1', 'Second', 'D:\\Media\\More');
    });
  });

  it('shows the API validation message when a folder path does not exist', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.addSource.mockRejectedValue(
      new ApiError("Folder 'D:\\Nope' does not exist. Choose an existing directory.", 400),
    );

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));
    await screen.findByText('No folders connected yet.', { exact: false });

    await user.type(screen.getByLabelText('Source name'), 'Broken');
    await user.type(screen.getByLabelText('Folder path'), 'D:\\Nope');
    await user.click(screen.getByRole('button', { name: 'Add folder' }));

    expect(
      await screen.findByText("Folder 'D:\\Nope' does not exist. Choose an existing directory."),
    ).toBeInTheDocument();
  });

  it('starts a scan and shows live progress until completion', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchSources.mockResolvedValue([mainSource]);
    apiMocks.startScan.mockResolvedValue(scanSnapshot({ status: 'Running', phase: 'Indexing', filesDiscovered: 120, filesProcessed: 40 }));

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));

    await user.click(await screen.findByRole('button', { name: 'Scan folder Main' }));

    await waitFor(() => {
      expect(startScan).toHaveBeenCalledWith('src-1');
    });
    expect(await screen.findByRole('status', { name: 'Scan progress' })).toBeInTheDocument();
    expect(screen.getByText('40 of 120 files processed')).toBeInTheDocument();

    // Poll 1: still indexing. Poll 2: completed.
    apiMocks.fetchScan
      .mockResolvedValueOnce(scanSnapshot({ status: 'Running', phase: 'Indexing', filesDiscovered: 120, filesProcessed: 90, filesIndexed: 90 }))
      .mockResolvedValueOnce(scanSnapshot({
        status: 'Completed', phase: 'Done',
        filesDiscovered: 120, filesProcessed: 120, filesIndexed: 118, filesUpdated: 2, filesUnchanged: 0, filesMarkedMissing: 0,
        completedAtUtc: '2026-09-07T12:01:00Z',
      }));

    expect(await screen.findByText('Last scan: 118 new, 2 updated, 0 unchanged, 0 missing.', undefined, { timeout: 3000 })).toBeInTheDocument();
    expect(cancelScan).not.toHaveBeenCalled();
  });

  it('cancels a running scan and reports the cancelled state', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchSources.mockResolvedValue([mainSource]);
    apiMocks.startScan.mockResolvedValue(scanSnapshot({ status: 'Running', phase: 'Indexing', filesDiscovered: 500, filesProcessed: 100 }));
    apiMocks.cancelScan.mockResolvedValue({ cancelled: true });

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));
    await user.click(await screen.findByRole('button', { name: 'Scan folder Main' }));
    await screen.findByRole('status', { name: 'Scan progress' });

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    await waitFor(() => {
      expect(cancelScan).toHaveBeenCalledWith('scan-1');
    });
    apiMocks.fetchScan.mockResolvedValue(
      scanSnapshot({ status: 'Cancelled', phase: 'Done', completedAtUtc: '2026-09-07T12:00:30Z' }),
    );
    expect(await screen.findByText('Last scan was cancelled.', undefined, { timeout: 3000 })).toBeInTheDocument();
  });

  it('shows an error when the scan API reports a failure', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchSources.mockResolvedValue([mainSource]);
    apiMocks.startScan.mockResolvedValue(scanSnapshot({ status: 'Running', phase: 'Discovering' }));

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));
    await user.click(await screen.findByRole('button', { name: 'Scan folder Main' }));

    apiMocks.fetchScan.mockResolvedValue(
      scanSnapshot({ status: 'Failed', phase: 'Done', error: "Source folder 'D:\\Gone' does not exist.", completedAtUtc: '2026-09-07T12:00:10Z' }),
    );

    expect(
      await screen.findByText('Last scan failed: Source folder \'D:\\Gone\' does not exist.'),
    ).toBeInTheDocument();
  });

  it('shows the 409 message when a scan is already running for the source', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchSources.mockResolvedValue([mainSource]);
    apiMocks.startScan.mockRejectedValue(new ApiError('A scan is already running for this source.', 409));

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));
    await user.click(await screen.findByRole('button', { name: 'Scan folder Main' }));

    expect(
      await screen.findByText('A scan is already running for this source.'),
    ).toBeInTheDocument();
  });

  it('lists indexed media items after a scan', async () => {
    const user = userEvent.setup();
    apiMocks.fetchLibraries.mockResolvedValue([musicLibrary]);
    apiMocks.fetchSources.mockResolvedValue([mainSource]);
    apiMocks.fetchLibraryItems.mockResolvedValue({
      items: [
        { id: 'item-1', displayName: '01 Sunrise', createdAtUtc: '2026-09-07T12:00:00Z' },
        { id: 'item-2', displayName: '02 Night Drive', createdAtUtc: '2026-09-07T12:00:00Z' },
      ],
      page: 1,
      pageSize: 50,
      totalCount: 2,
    });

    renderApp('/library');
    await user.click(await screen.findByRole('button', { name: 'Folders' }));

    expect(await screen.findByText('01 Sunrise')).toBeInTheDocument();
    expect(screen.getByText('02 Night Drive')).toBeInTheDocument();
    expect(screen.getByText('(2)', { exact: false })).toBeInTheDocument();
  });

  it('shows a retryable error state when libraries cannot be loaded', async () => {
    apiMocks.fetchLibraries.mockRejectedValue(new ApiError('The oniDash API is unreachable.', 0));

    renderApp('/library');

    expect(
      await screen.findByText('Libraries could not be loaded'),
    ).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument();
  });
});
