using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using oniDash.Application.Catalogue;

namespace oniDash.Infrastructure.Windows;

public sealed record WindowsAppConfig(
    bool LaunchOnStartup,
    bool MinimizeToTray,
    bool CloseToTray,
    bool GlobalMediaKeysEnabled,
    bool NotificationsEnabled
);

public sealed record WindowsNotificationMessage(
    string Title,
    string Message,
    DateTimeOffset TimestampUtc
);

public interface IWindowsProductService : IDesktopHost, ITrayIntegration, IFileAssociationRegistrar, IBackupService
{
    WindowsAppConfig GetConfig();
    void UpdateConfig(WindowsAppConfig config);
    IReadOnlyList<WindowsNotificationMessage> GetNotificationHistory();
    Task HandleMediaKeyAsync(string action, CancellationToken cancellationToken = default);
}

public sealed class WindowsProductService : IWindowsProductService
{
    private readonly string _dataDirectory;
    private readonly List<WindowsNotificationMessage> _notifications = new();
    private readonly HashSet<string> _registeredExtensions = new(StringComparer.OrdinalIgnoreCase);
    private WindowsAppConfig _config;
    private bool _isRunning;
    private bool _isTrayVisible = true;

    public WindowsProductService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "oniDash");
        _config = new WindowsAppConfig(
            LaunchOnStartup: false,
            MinimizeToTray: true,
            CloseToTray: true,
            GlobalMediaKeysEnabled: true,
            NotificationsEnabled: true
        );
    }

    public bool IsRunning => _isRunning;
    public bool IsTrayVisible => _isTrayVisible;
    public IReadOnlyCollection<string> RegisteredExtensions => _registeredExtensions;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public WindowsAppConfig GetConfig() => _config;

    public void UpdateConfig(WindowsAppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task SetVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        _isTrayVisible = visible;
        return Task.CompletedTask;
    }

    public Task ShowNotificationAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        if (_config.NotificationsEnabled)
        {
            _notifications.Add(new WindowsNotificationMessage(title, message, DateTimeOffset.UtcNow));
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<WindowsNotificationMessage> GetNotificationHistory() => _notifications.AsReadOnly();

    public Task RegisterAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default)
    {
        foreach (var ext in extensions)
        {
            var normalized = ext.StartsWith('.') ? ext : "." + ext;
            _registeredExtensions.Add(normalized.ToLowerInvariant());
        }
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(IReadOnlyCollection<string> extensions, CancellationToken cancellationToken = default)
    {
        foreach (var ext in extensions)
        {
            var normalized = ext.StartsWith('.') ? ext : "." + ext;
            _registeredExtensions.Remove(normalized.ToLowerInvariant());
        }
        return Task.CompletedTask;
    }

    public Task HandleMediaKeyAsync(string action, CancellationToken cancellationToken = default)
    {
        if (!_config.GlobalMediaKeysEnabled) return Task.CompletedTask;
        // Standard Windows Media Keys: Play, Pause, Next, Previous, Stop
        return Task.CompletedTask;
    }

    public async Task ExportAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        if (Directory.Exists(_dataDirectory))
        {
            var files = Directory.GetFiles(_dataDirectory, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relative = Path.GetRelativePath(_dataDirectory, file);
                var entry = archive.CreateEntry(relative);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(file);
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }
        }
        else
        {
            var manifestEntry = archive.CreateEntry("manifest.json");
            await using var ms = manifestEntry.Open();
            await using var writer = new StreamWriter(ms);
            await writer.WriteAsync("{\"version\":\"1.0.0\",\"backupDate\":\"" + DateTimeOffset.UtcNow.ToString("O") + "\"}");
        }
    }

    public async Task ImportAsync(Stream source, CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        Directory.CreateDirectory(_dataDirectory);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // directory
            var targetPath = Path.Combine(_dataDirectory, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await using var entryStream = entry.Open();
            await using var targetStream = File.Create(targetPath);
            await entryStream.CopyToAsync(targetStream, cancellationToken);
        }
    }
}
