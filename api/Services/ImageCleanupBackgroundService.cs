using Dapper;
using PCS_API.Repositories;

namespace PCS_API.Services;

public class ImageCleanupBackgroundService(
        ImageCleanupChannel channel,
        IWebHostEnvironment env,
        ISqlConnectionFactory connectionFactory,
        ILogger<ImageCleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var filesToCheck in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var distinctFiles = filesToCheck.Distinct().ToList();
                if (!distinctFiles.Any()) continue;

                using var conn = connectionFactory.CreateConnection();
                var usedImages = await conn.QueryAsync<string>(
                    "SELECT DISTINCT ImageUrl FROM ProductVariants WHERE ImageUrl IN @Urls",
                    new { Urls = distinctFiles });

                var filesToDelete = distinctFiles.Except(usedImages).ToList();
                var webRootPath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        var fullPath = Path.Combine(webRootPath, file.TrimStart('/'));
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            logger.LogInformation("Deleted orphan image: {Path}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to delete orphan image: {Path}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during image cleanup batch");
            }
        }
    }
}
