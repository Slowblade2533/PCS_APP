using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace PCS_API.Services
{
    public class TempFileCleanupBackgroundService : BackgroundService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<TempFileCleanupBackgroundService> _logger;
        private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(24);

        public TempFileCleanupBackgroundService(
            IWebHostEnvironment env,
            ILogger<TempFileCleanupBackgroundService> logger)
        {
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TempFileCleanupBackgroundService has started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupTempFiles();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up temporary files.");
                }

                try
                {
                    await Task.Delay(RunInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Normal shutdown
                    break;
                }
            }

            _logger.LogInformation("TempFileCleanupBackgroundService has stopped.");
        }

        private void CleanupTempFiles()
        {
            var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var tempFolder = Path.Combine(webRootPath, "uploads", "temp");

            if (!Directory.Exists(tempFolder))
            {
                _logger.LogInformation("Temp upload directory {Path} does not exist. Skipping cleanup.", tempFolder);
                return;
            }

            _logger.LogInformation("Scanning temp upload directory {Path} for old files...", tempFolder);

            var files = Directory.GetFiles(tempFolder);
            var now = DateTime.UtcNow;
            int deletedCount = 0;

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    // Use UTC time comparison for consistency
                    var fileAge = now - fileInfo.LastWriteTimeUtc;

                    if (fileAge > MaxFileAge)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogInformation("Deleted expired temp file: {FileName} (Age: {Age:hh\\:mm\\:ss})", fileInfo.Name, fileAge);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to inspect/delete temp file: {Path}", file);
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation("Successfully deleted {Count} expired temporary files.", deletedCount);
            }
            else
            {
                _logger.LogInformation("No expired temporary files found.");
            }
        }
    }
}
