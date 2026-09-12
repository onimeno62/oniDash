using System;
using System.IO;
using System.Threading.Tasks;
using oniDash.Infrastructure.Windows;
using Xunit;

namespace oniDash.Infrastructure.Tests;

public class WindowsProductServiceTests
{
    [Fact]
    public async Task WindowsProductService_Config_Notifications_And_Associations_Work()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"onidash_win_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var service = new WindowsProductService(tempDir);
            await service.StartAsync();
            Assert.True(service.IsRunning);

            var initialConfig = service.GetConfig();
            Assert.False(initialConfig.LaunchOnStartup);

            service.UpdateConfig(initialConfig with { LaunchOnStartup = true, GlobalMediaKeysEnabled = true });
            Assert.True(service.GetConfig().LaunchOnStartup);

            await service.ShowNotificationAsync("Test Alert", "Sample Message");
            var history = service.GetNotificationHistory();
            Assert.Single(history);
            Assert.Equal("Test Alert", history[0].Title);

            await service.RegisterAsync(new[] { ".mp3", "flac", ".epub" });
            Assert.Contains(".mp3", service.RegisteredExtensions);
            Assert.Contains(".flac", service.RegisteredExtensions);
            Assert.Contains(".epub", service.RegisteredExtensions);

            await service.UnregisterAsync(new[] { ".mp3" });
            Assert.DoesNotContain(".mp3", service.RegisteredExtensions);

            // Backup export
            using var ms = new MemoryStream();
            await service.ExportAsync(ms);
            Assert.True(ms.Length > 0);

            await service.StopAsync();
            Assert.False(service.IsRunning);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
