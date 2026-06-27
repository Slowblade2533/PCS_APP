namespace PCS_API.Services
{
    public class TempFileCleanupBackgroundService(
            IWebHostEnvironment env,
            ILogger<TempFileCleanupBackgroundService> logger) : BackgroundService
    {
        private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);
        private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(24);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("TempFileCleanupBackgroundService has started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupTempFiles();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while cleaning up temporary files.");
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

            logger.LogInformation("TempFileCleanupBackgroundService has stopped.");
        }

        private void CleanupTempFiles()
        {
            var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var tempFolder = Path.Combine(webRootPath, "uploads", "temp");

            if (!Directory.Exists(tempFolder))
            {
                logger.LogInformation("Temp upload directory {Path} does not exist. Skipping cleanup.", tempFolder);
                return;
            }

            logger.LogInformation("Scanning temp upload directory {Path} for old files...", tempFolder);

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
                        logger.LogInformation("Deleted expired temp file: {FileName} (Age: {Age:hh\\:mm\\:ss})", fileInfo.Name, fileAge);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to inspect/delete temp file: {Path}", file);
                }
            }

            if (deletedCount > 0)
            {
                logger.LogInformation("Successfully deleted {Count} expired temporary files.", deletedCount);
            }
            else
            {
                logger.LogInformation("No expired temporary files found.");
            }
        }
    }
}
