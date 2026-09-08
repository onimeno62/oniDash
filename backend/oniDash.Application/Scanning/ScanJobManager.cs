using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace oniDash.Application.Scanning;

/// <summary>
/// Runs scans as background work: one scan at a time per source, thread-safe snapshots,
/// cancellation by id. Each run resolves a fresh <see cref="IScanService"/> from its own
/// DI scope (the manager is a singleton; the service and its repositories are scoped).
/// Scans are tracked in memory only — a process restart clears the history, while the
/// persisted index stays intact.
/// </summary>
public sealed class ScanJobManager(IServiceScopeFactory scopeFactory) : IScanJobManager, IDisposable
{
    private sealed class ScanRun
    {
        public required ScanProgress Progress;
        public CancellationTokenSource Cancellation = new();
        public Task Task = Task.CompletedTask;
    }

    private readonly object _gate = new();
    private readonly Dictionary<Guid, ScanRun> _runsById = new();
    private readonly Dictionary<Guid, Guid> _activeBySource = new();
    private readonly List<Guid> _order = new();
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public StartScanResult StartScan(Guid libraryId, Guid sourceId, string sourceName)
    {
        lock (_gate)
        {
            if (_activeBySource.TryGetValue(sourceId, out var activeId))
            {
                return new StartScanResult(false, activeId, "A scan is already running for this source.");
            }

            var scanId = Guid.NewGuid();
            var run = new ScanRun
            {
                Progress = new ScanProgress(
                    scanId, libraryId, sourceId, sourceName,
                    ScanStatus.Running, ScanPhase.Discovering,
                    0, 0, 0, 0, 0, 0, DateTimeOffset.UtcNow, null, null),
            };

            _runsById[scanId] = run;
            _activeBySource[sourceId] = scanId;
            _order.Add(scanId);
            run.Task = ExecuteAsync(run);

            return new StartScanResult(true, scanId, null);
        }
    }

    private Task ExecuteAsync(ScanRun run)
    {
        return Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scanService = scope.ServiceProvider.GetRequiredService<IScanService>();

                var outcome = await scanService
                    .ScanSourceAsync(
                        run.Progress.SourceId,
                        update => UpdateProgress(run, status: null, phase: null, update),
                        run.Cancellation.Token)
                    .ConfigureAwait(false);

                UpdateProgress(run, ScanStatus.Completed, ScanPhase.Done, new ScanProgressUpdate(
                    ScanPhase.Done,
                    outcome.Discovered,
                    outcome.Discovered,
                    outcome.Indexed,
                    outcome.Updated,
                    outcome.Unchanged,
                    outcome.MarkedMissing), completed: true);
            }
            catch (OperationCanceledException)
            {
                UpdateProgress(run, ScanStatus.Cancelled, ScanPhase.Done, null, completed: true);
            }
            catch (Exception ex)
            {
                UpdateProgress(run, ScanStatus.Failed, null, null, ex.Message, completed: true);
            }
            finally
            {
                lock (_gate)
                {
                    if (_activeBySource.TryGetValue(run.Progress.SourceId, out var active)
                        && active == run.Progress.ScanId)
                    {
                        _activeBySource.Remove(run.Progress.SourceId);
                    }
                }
            }
        });
    }

    public ScanProgress? GetScan(Guid scanId)
    {
        lock (_gate)
        {
            return _runsById.TryGetValue(scanId, out var run) ? run.Progress : null;
        }
    }

    public bool TryCancel(Guid scanId)
    {
        ScanRun? run;
        lock (_gate)
        {
            if (!_runsById.TryGetValue(scanId, out run) || run.Progress.Status != ScanStatus.Running)
            {
                return false;
            }
        }

        run.Cancellation.Cancel();
        return true;
    }

    public IReadOnlyList<ScanProgress> ListScans(Guid? sourceId = null, int limit = 20)
    {
        lock (_gate)
        {
            return _order
                .Select(id => _runsById[id].Progress)
                .Where(progress => sourceId == null || progress.SourceId == sourceId)
                .TakeLast(limit)
                .ToList();
        }
    }

    public void Dispose()
    {
        ScanRun[] runs;
        lock (_gate)
        {
            runs = _runsById.Values.ToArray();
        }

        foreach (var run in runs)
        {
            try
            {
                run.Cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        try
        {
            Task.WaitAll(runs.Select(run => run.Task).ToArray(), TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Dispose is best-effort; individual failures are already captured per run.
        }
    }

    private void UpdateProgress(
        ScanRun run,
        ScanStatus? status,
        ScanPhase? phase,
        ScanProgressUpdate? update,
        string? error = null,
        bool completed = false)
    {
        lock (_gate)
        {
            var current = run.Progress;
            run.Progress = current with
            {
                Status = status ?? current.Status,
                Phase = phase ?? update?.Phase ?? current.Phase,
                FilesDiscovered = update?.FilesDiscovered ?? current.FilesDiscovered,
                FilesProcessed = update?.FilesProcessed ?? current.FilesProcessed,
                FilesIndexed = update?.FilesIndexed ?? current.FilesIndexed,
                FilesUpdated = update?.FilesUpdated ?? current.FilesUpdated,
                FilesUnchanged = update?.FilesUnchanged ?? current.FilesUnchanged,
                FilesMarkedMissing = update?.FilesMarkedMissing ?? current.FilesMarkedMissing,
                Error = error ?? current.Error,
                // Terminal status and completion timestamp land in the same locked write,
                // so pollers never observe a terminal state without its timestamp.
                CompletedAtUtc = completed ? DateTimeOffset.UtcNow : current.CompletedAtUtc,
            };
        }
    }
}
