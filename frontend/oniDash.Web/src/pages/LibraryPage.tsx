import { useEffect, useState, type FormEvent } from 'react';
import { PageHeader } from '../components/PageHeader';
import { ErrorState } from '../components/states/ErrorState';
import { LoadingState } from '../components/states/LoadingState';
import {
  CheckIcon,
  CloseIcon,
  FolderIcon,
  LibraryIcon,
  PencilIcon,
  PlusIcon,
  RefreshIcon,
  TrashIcon,
} from '../components/icons';
import { fetchLibraryItems, type MediaItemSummary } from '../api/libraries';
import { isTerminalStatus, type ScanJobSnapshot } from '../api/scans';
import { useLibraries, useLibrarySources } from '../hooks/useLibraries';
import { useScan } from '../hooks/useScan';

/**
 * Library management: create libraries, rename or delete them, connect read-only folder
 * sources, scan those folders in the background, and browse the discovered media.
 */
export function LibraryPage() {
  const { libraries, loading, error, reload, create, rename, remove } = useLibraries();
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [itemsVersion, setItemsVersion] = useState(0);

  const creating = showCreateForm || libraries.length === 0;
  // When the empty state offers the create form there is no close affordance; the
  // dedicated toggle only exists once at least one library exists.
  const showForm = showCreateForm || libraries.length === 0;

  return (
    <div className="space-y-8">
      <PageHeader
        title="Library"
        subtitle="Create libraries, connect read-only folder sources, and scan them to build your media index. oniDash never modifies the files themselves."
        actions={
          libraries.length > 0 && (
            <button
              type="button"
              onClick={() => {
                setShowCreateForm((previous) => !previous);
                setExpandedId(null);
              }}
              className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-medium text-white shadow-card transition-colors hover:bg-accent-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              <PlusIcon className="size-4" />
              New library
            </button>
          )
        }
      />

      {loading && <LoadingState label="Loading libraries…" />}

      {!loading && error && (
        <ErrorState
          title="Libraries could not be loaded"
          message={error}
          onRetry={() => void reload()}
        />
      )}

      {!loading && !error && showForm && (
        <LibraryForm
          mode={creating && libraries.length === 0 ? 'create-first' : 'create'}
          onSubmit={async (name) => {
            await create(name);
            setShowCreateForm(false);
          }}
          onCancel={libraries.length === 0 ? undefined : () => setShowCreateForm(false)}
        />
      )}

      {!loading && !error && libraries.length > 0 && (
        <section aria-label="Libraries" className="space-y-4">
          {libraries.map((library) => (
            <div key={library.id} className="space-y-3">
              <LibraryRow
                name={library.name}
                createdAtUtc={library.createdAtUtc}
                expanded={expandedId === library.id}
                onToggle={() =>
                  setExpandedId((previous) => (previous === library.id ? null : library.id))
                }
                onRename={async (name) => {
                  await rename(library.id, name);
                }}
                onDelete={async () => {
                  await remove(library.id);
                  if (expandedId === library.id) {
                    setExpandedId(null);
                  }
                }}
              />
              {expandedId === library.id && (
                <>
                  <SourcesSection
                    libraryId={library.id}
                    onScanCompleted={() => setItemsVersion((version) => version + 1)}
                  />
                  <ItemsSection libraryId={library.id} version={itemsVersion} />
                </>
              )}
            </div>
          ))}
        </section>
      )}
    </div>
  );
}

interface LibraryRowProps {
  name: string;
  createdAtUtc: string;
  expanded: boolean;
  onToggle: () => void;
  onRename: (name: string) => Promise<void>;
  onDelete: () => Promise<void>;
}

function LibraryRow({
  name,
  createdAtUtc,
  expanded,
  onToggle,
  onRename,
  onDelete,
}: LibraryRowProps) {
  const [editing, setEditing] = useState(false);
  const [editName, setEditName] = useState(name);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const created = new Date(createdAtUtc).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });

  return (
    <article className="rounded-2xl border border-border bg-surface shadow-card">
      <div className="flex flex-wrap items-center gap-3 p-4 md:p-5">
        <div
          aria-hidden
          className="grid size-10 shrink-0 place-items-center rounded-lg bg-accent-soft text-accent"
        >
          <LibraryIcon className="size-5" />
        </div>

        {editing ? (
          <form
            className="flex min-w-0 flex-1 items-center gap-2"
            onSubmit={async (event: FormEvent) => {
              event.preventDefault();
              setActionError(null);
              try {
                await onRename(editName);
                setEditing(false);
              } catch (err) {
                setActionError(err instanceof Error ? err.message : 'Rename failed.');
              }
            }}
          >
            <input
              aria-label="Library name"
              value={editName}
              onChange={(event) => setEditName(event.target.value)}
              className="min-w-0 flex-1 rounded-lg border border-border bg-surface-elevated px-3 py-2 text-sm text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            />
            <button
              type="submit"
              aria-label="Save library name"
              className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-accent-hover"
            >
              <CheckIcon className="size-4" />
              Save
            </button>
            <button
              type="button"
              aria-label="Cancel renaming"
              onClick={() => {
                setEditing(false);
                setEditName(name);
                setActionError(null);
              }}
              className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-surface px-3 py-2 text-sm font-medium text-primary transition-colors hover:bg-surface-hover"
            >
              <CloseIcon className="size-4" />
              Cancel
            </button>
          </form>
        ) : (
          <div className="min-w-0 flex-1">
            <p className="truncate text-[15px] font-medium">{name}</p>
            <p className="text-xs text-muted">Created {created}</p>
          </div>
        )}

        <div className="flex items-center gap-1.5">
          <button
            type="button"
            onClick={onToggle}
            aria-expanded={expanded}
            className="rounded-lg border border-border bg-surface px-3 py-2 text-sm font-medium text-primary transition-colors hover:bg-surface-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            {expanded ? 'Hide folders' : 'Folders'}
          </button>
          <button
            type="button"
            aria-label={`Rename library ${name}`}
            title="Rename"
            onClick={() => {
              setEditing(true);
              setActionError(null);
            }}
            className="rounded-lg border border-border bg-surface p-2 text-secondary transition-colors hover:bg-surface-hover hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
          >
            <PencilIcon className="size-4" />
          </button>
          {confirmingDelete ? (
            <span className="flex items-center gap-1.5">
              <button
                type="button"
                onClick={async () => {
                  try {
                    await onDelete();
                  } catch (err) {
                    setActionError(err instanceof Error ? err.message : 'Delete failed.');
                    setConfirmingDelete(false);
                  }
                }}
                className="rounded-lg bg-danger px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-danger-hover"
              >
                Delete
              </button>
              <button
                type="button"
                aria-label="Cancel deleting library"
                onClick={() => setConfirmingDelete(false)}
                className="rounded-lg border border-border bg-surface p-2 text-secondary transition-colors hover:bg-surface-hover hover:text-primary"
              >
                <CloseIcon className="size-4" />
              </button>
            </span>
          ) : (
            <button
              type="button"
              aria-label={`Delete library ${name}`}
              title="Delete"
              onClick={() => setConfirmingDelete(true)}
              className="rounded-lg border border-border bg-surface p-2 text-secondary transition-colors hover:bg-danger/10 hover:text-danger focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              <TrashIcon className="size-4" />
            </button>
          )}
        </div>
      </div>

      {actionError && (
        <p role="alert" className="px-4 pb-4 text-sm text-danger md:px-5">
          {actionError}
        </p>
      )}
    </article>
  );
}

interface LibraryFormProps {
  mode: 'create' | 'create-first';
  onSubmit: (name: string) => Promise<void>;
  onCancel?: () => void;
}

export function LibraryForm({ mode, onSubmit, onCancel }: LibraryFormProps) {
  const [name, setName] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  return (
    <section
      aria-label={mode === 'create-first' ? 'Create your first library' : 'Create a new library'}
      className="rounded-2xl border border-border bg-surface p-5 shadow-card md:p-6"
    >
      <h3 className="text-base font-semibold tracking-tight">
        {mode === 'create-first' ? 'Create your first library' : 'Create a new library'}
      </h3>
      <p className="mt-1 text-sm text-secondary">
        A library groups folders and their media. It is only an index — oniDash never
        modifies the files inside your folders.
      </p>
      <form
        className="mt-4 flex flex-wrap items-center gap-2"
        onSubmit={async (event: FormEvent) => {
          event.preventDefault();
          if (submitting) return;
          setSubmitting(true);
          setFormError(null);
          try {
            await onSubmit(name);
            setName('');
          } catch (err) {
            setFormError(err instanceof Error ? err.message : 'The library could not be created.');
          } finally {
            setSubmitting(false);
          }
        }}
      >
        <input
          aria-label="Library name"
          placeholder="For example: Music, Movies"
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="min-w-0 flex-1 rounded-lg border border-border bg-surface-elevated px-3 py-2.5 text-sm text-primary placeholder:text-muted focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        />
        <button
          type="submit"
          disabled={submitting}
          className="inline-flex items-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-medium text-white shadow-card transition-colors hover:bg-accent-hover disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          <PlusIcon className="size-4" />
          {submitting ? 'Creating…' : 'Create library'}
        </button>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg border border-border bg-surface px-4 py-2.5 text-sm font-medium text-primary transition-colors hover:bg-surface-hover"
          >
            Cancel
          </button>
        )}
      </form>
      {formError && (
        <p role="alert" className="mt-3 text-sm text-danger">
          {formError}
        </p>
      )}
    </section>
  );
}

interface SourcesSectionProps {
  libraryId: string;
  onScanCompleted: () => void;
}

function SourcesSection({ libraryId, onScanCompleted }: SourcesSectionProps) {
  const { sources, loading, error, add, remove } = useLibrarySources(libraryId);
  const [name, setName] = useState('');
  const [path, setPath] = useState('');
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const { job, starting, error: scanError, start, cancel } = useScan(onScanCompleted);

  const scanActiveFor = (sourceId: string) =>
    job !== null && job.sourceId === sourceId && !isTerminalStatus(job.status);

  return (
    <section
      aria-label="Folder sources"
      className="rounded-2xl border border-border bg-surface-elevated p-4 md:p-5"
    >
      <h4 className="text-sm font-semibold tracking-tight">Folder sources</h4>
      <p className="mt-1 text-sm text-secondary">
        Folders oniDash is allowed to index. Contents are read-only — scanning only builds
        the index and never creates, moves, or deletes files.
      </p>

      <form
        className="mt-4 flex flex-wrap items-center gap-2"
        onSubmit={async (event: FormEvent) => {
          event.preventDefault();
          if (adding) return;
          setAdding(true);
          setAddError(null);
          try {
            await add(name, path);
            setName('');
            setPath('');
          } catch (err) {
            setAddError(err instanceof Error ? err.message : 'The folder could not be added.');
          } finally {
            setAdding(false);
          }
        }}
      >
        <input
          aria-label="Source name"
          placeholder="Name, for example: Main music folder"
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="w-48 min-w-0 rounded-lg border border-border bg-surface px-3 py-2.5 text-sm text-primary placeholder:text-muted focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        />
        <input
          aria-label="Folder path"
          placeholder="Windows path, for example: D:\Media\Music"
          value={path}
          onChange={(event) => setPath(event.target.value)}
          className="min-w-0 flex-1 rounded-lg border border-border bg-surface px-3 py-2.5 font-mono text-sm text-primary placeholder:text-muted focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        />
        <button
          type="submit"
          disabled={adding}
          className="inline-flex items-center gap-2 rounded-lg bg-accent px-3.5 py-2.5 text-sm font-medium text-white shadow-card transition-colors hover:bg-accent-hover disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          <PlusIcon className="size-4" />
          {adding ? 'Adding…' : 'Add folder'}
        </button>
      </form>

      {addError && (
        <p role="alert" className="mt-3 text-sm text-danger">
          {addError}
        </p>
      )}

      {loading && (
        <p role="status" className="mt-4 text-sm text-secondary">
          Loading folders…
        </p>
      )}

      {!loading && error && (
        <p role="alert" className="mt-4 text-sm text-danger">
          {error}
        </p>
      )}

      {!loading && !error && sources.length === 0 && (
        <p className="mt-4 text-sm text-muted">
          No folders connected yet. Add an existing folder above — oniDash will never create,
          move, or delete files inside it.
        </p>
      )}

      {!loading && sources.length > 0 && (
        <ul className="mt-4 space-y-2">
          {sources.map((source) => (
            <li
              key={source.id}
              className="flex items-center gap-3 rounded-xl border border-border bg-surface px-3 py-2.5"
            >
              <span
                aria-hidden
                className="grid size-8 shrink-0 place-items-center rounded-lg bg-accent-soft text-accent"
              >
                <FolderIcon className="size-4" />
              </span>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-medium">{source.name}</span>
                <span className="block truncate font-mono text-xs text-muted">
                  {source.rootPath}
                </span>
              </span>
              <button
                type="button"
                aria-label={`Scan folder ${source.name}`}
                disabled={starting || (job !== null && !isTerminalStatus(job.status))}
                onClick={() => void start(source.id)}
                className="inline-flex items-center gap-1.5 rounded-lg border border-border bg-surface px-3 py-2 text-sm font-medium text-primary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-60 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
              >
                <RefreshIcon className="size-4" />
                {scanActiveFor(source.id) ? 'Scanning…' : 'Scan'}
              </button>
              <button
                type="button"
                aria-label={`Remove folder ${source.name}`}
                title="Remove source"
                onClick={async () => {
                  try {
                    await remove(source.id);
                  } catch (err) {
                    setAddError(err instanceof Error ? err.message : 'The folder could not be removed.');
                  }
                }}
                className="rounded-lg border border-border bg-surface p-2 text-secondary transition-colors hover:bg-danger/10 hover:text-danger focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
              >
                <TrashIcon className="size-4" />
              </button>
            </li>
          ))}
        </ul>
      )}

      {job && !isTerminalStatus(job.status) && (
        <ScanProgressCard job={job} onCancel={() => void cancel()} />
      )}

      {job && isTerminalStatus(job.status) && (
        <p
          role="status"
          className="mt-3 text-sm text-secondary"
          data-testid="scan-result"
        >
          {job.status === 'Completed'
            ? `Last scan: ${job.filesIndexed} new, ${job.filesUpdated} updated, ${job.filesUnchanged} unchanged, ${job.filesMarkedMissing} missing.`
            : job.status === 'Cancelled'
              ? 'Last scan was cancelled.'
              : `Last scan failed: ${job.error ?? 'unknown error'}`}
        </p>
      )}

      {scanError && (
        <p role="alert" className="mt-3 text-sm text-danger">
          {scanError}
        </p>
      )}
    </section>
  );
}

function ScanProgressCard({ job, onCancel }: { job: ScanJobSnapshot; onCancel: () => void }) {
  const total = job.filesDiscovered || 1;
  const processed = job.phase === 'Discovering' ? 0 : Math.min(job.filesProcessed, total);
  const percent = Math.round((processed / total) * 100);

  return (
    <div
      role="status"
      aria-label="Scan progress"
      className="mt-3 rounded-xl border border-border bg-surface p-3"
      data-testid="scan-progress"
    >
      <div className="flex items-center justify-between gap-3">
        <span className="text-sm font-medium">
          {job.phase === 'Discovering' ? 'Discovering files…' : 'Indexing files…'}
        </span>
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-border bg-surface px-3 py-1.5 text-sm font-medium text-primary transition-colors hover:bg-surface-hover focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
        >
          Cancel
        </button>
      </div>
      <div
        aria-label="Scan progress bar"
        className="mt-2 h-1.5 overflow-hidden rounded-full bg-surface-hover"
      >
        <div
          className="h-full rounded-full bg-accent transition-all"
          style={{ width: `${percent}%` }}
        />
      </div>
      <p className="mt-2 text-xs text-muted">
        {job.phase === 'Discovering'
          ? `${job.filesDiscovered} files found`
          : `${processed} of ${job.filesDiscovered} files processed`}
      </p>
    </div>
  );
}

interface ItemsSectionProps {
  libraryId: string;
  version: number;
}

function ItemsSection({ libraryId, version }: ItemsSectionProps) {
  const [items, setItems] = useState<MediaItemSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    fetchLibraryItems(libraryId, 1, 50, controller.signal)
      .then((page) => {
        setItems(page.items);
        setTotalCount(page.totalCount);
        setError(null);
      })
      .catch((err: unknown) => {
        if ((err as { name?: string })?.name === 'AbortError') return;
        setError(err instanceof Error ? err.message : 'Media could not be loaded.');
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [libraryId, version]);

  return (
    <section aria-label="Media in this library" className="rounded-2xl border border-border bg-surface-elevated p-4 md:p-5">
      <h4 className="text-sm font-semibold tracking-tight">
        Media {totalCount > 0 && <span className="text-muted">({totalCount})</span>}
      </h4>
      {loading && (
        <p role="status" className="mt-3 text-sm text-secondary">
          Loading media…
        </p>
      )}
      {error && (
        <p role="alert" className="mt-3 text-sm text-danger">
          {error}
        </p>
      )}
      {!loading && !error && items.length === 0 && (
        <p className="mt-3 text-sm text-muted">
          No media indexed yet. Scan a folder above to discover files.
        </p>
      )}
      {!loading && !error && items.length > 0 && (
        <ul className="mt-3 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <li
              key={item.id}
              className="flex items-center gap-3 rounded-xl border border-border bg-surface px-3 py-2.5"
            >
              <span
                aria-hidden
                className="grid size-8 shrink-0 place-items-center rounded-lg bg-accent-soft text-accent"
              >
                <LibraryIcon className="size-4" />
              </span>
              <span className="min-w-0 truncate text-sm font-medium">{item.displayName}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
