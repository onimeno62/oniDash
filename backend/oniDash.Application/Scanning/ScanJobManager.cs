using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace oniDash.Application.Scanning;

/// <summary>In-process scan runner with per-source serialization, progress, cancellation, retry and diagnostics.</summary>
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
                return new StartScanResult(false, activeId, "A scan is already running for this source.");
            var scanId = Guid.NewGuid();
            var run = new ScanRun { Progress = new ScanProgress(scanId, libraryId, sourceId, sourceName, ScanStatus.Running,
                ScanPhase.Discovering, 0, 0, 0, 0, 0, 0, DateTimeOffset.UtcNow, null, null, []) };
            _runsById[scanId] = run; _activeBySource[sourceId] = scanId; _order.Add(scanId); run.Task = ExecuteAsync(run);
            return new StartScanResult(true, scanId, null);
        }
    }

    public bool TryRetry(Guid scanId, out Guid retryScanId)
    {
        ScanProgress? previous;
        lock (_gate)
        {
            previous = _runsById.TryGetValue(scanId, out var run) ? run.Progress : null;
            if (previous is null || previous.Status == ScanStatus.Running || _activeBySource.ContainsKey(previous.SourceId))
            { retryScanId = Guid.Empty; return false; }
        }
        var result = StartScan(previous.LibraryId, previous.SourceId, previous.SourceName);
        retryScanId = result.Accepted ? result.ScanId : Guid.Empty;
        return result.Accepted;
    }

    private Task ExecuteAsync(ScanRun run) => Task.Run(async () =>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IScanService>();
            var outcome = await service.ScanSourceAsync(run.Progress.SourceId, u => UpdateProgress(run, null, null, u), run.Cancellation.Token).ConfigureAwait(false);
            UpdateProgress(run, ScanStatus.Completed, ScanPhase.Done,
                new ScanProgressUpdate(ScanPhase.Done, outcome.Discovered, outcome.Discovered, outcome.Indexed, outcome.Updated, outcome.Unchanged, outcome.MarkedMissing),
                completed: true, diagnostics: outcome.Diagnostics);
        }
        catch (OperationCanceledException) { UpdateProgress(run, ScanStatus.Cancelled, ScanPhase.Done, null, completed: true); }
        catch (Exception ex) { UpdateProgress(run, ScanStatus.Failed, null, null, ex.Message, completed: true); }
        finally
        {
            lock (_gate)
                if (_activeBySource.TryGetValue(run.Progress.SourceId, out var active) && active == run.Progress.ScanId) _activeBySource.Remove(run.Progress.SourceId);
        }
    });

    public ScanProgress? GetScan(Guid scanId) { lock (_gate) return _runsById.TryGetValue(scanId, out var run) ? run.Progress : null; }
    public bool TryCancel(Guid scanId)
    {
        ScanRun? run;
        lock (_gate) { if (!_runsById.TryGetValue(scanId, out run) || run.Progress.Status != ScanStatus.Running) return false; }
        run.Cancellation.Cancel(); return true;
    }
    public IReadOnlyList<ScanProgress> ListScans(Guid? sourceId = null, int limit = 20)
    {
        lock (_gate) return _order.Select(id => _runsById[id].Progress).Where(p => sourceId == null || p.SourceId == sourceId).TakeLast(Math.Clamp(limit, 1, 200)).ToList();
    }
    public void Dispose()
    {
        ScanRun[] runs; lock (_gate) runs = _runsById.Values.ToArray();
        foreach (var run in runs) run.Cancellation.Cancel();
        try { Task.WaitAll(runs.Select(r => r.Task).ToArray(), TimeSpan.FromSeconds(5)); } catch (AggregateException) { }
    }
    private void UpdateProgress(ScanRun run, ScanStatus? status, ScanPhase? phase, ScanProgressUpdate? update,
        string? error = null, bool completed = false, IReadOnlyList<ScanDiagnostic>? diagnostics = null)
    {
        lock (_gate)
        {
            var current = run.Progress;
            run.Progress = current with { Status = status ?? current.Status, Phase = phase ?? update?.Phase ?? current.Phase,
                FilesDiscovered = update?.FilesDiscovered ?? current.FilesDiscovered, FilesProcessed = update?.FilesProcessed ?? current.FilesProcessed,
                FilesIndexed = update?.FilesIndexed ?? current.FilesIndexed, FilesUpdated = update?.FilesUpdated ?? current.FilesUpdated,
                FilesUnchanged = update?.FilesUnchanged ?? current.FilesUnchanged, FilesMarkedMissing = update?.FilesMarkedMissing ?? current.FilesMarkedMissing,
                Error = error ?? current.Error, CompletedAtUtc = completed ? DateTimeOffset.UtcNow : current.CompletedAtUtc,
                Diagnostics = diagnostics ?? current.Diagnostics };
        }
    }
}
