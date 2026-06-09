using Dapper;
using Microsoft.Data.SqlClient;

namespace PCS_API.Services;

public class ImageCleanupBackgroundService : BackgroundService
{
    private readonly ImageCleanupChannel _channel;
    private readonly IWebHostEnvironment _env;
    private readonly string _connectionString;
    private readonly ILogger<ImageCleanupBackgroundService> _logger;

    public ImageCleanupBackgroundService(
        ImageCleanupChannel channel,
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<ImageCleanupBackgroundService> logger)
    {
        _channel = channel;
        _env = env;
        _connectionString = config.GetConnectionString("DefaultConnection")!;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var filesToCheck in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var distinctFiles = filesToCheck.Distinct().ToList();
                if (!distinctFiles.Any()) continue;

                await using var conn = new SqlConnection(_connectionString);
                var usedImages = await conn.QueryAsync<string>(
                    "SELECT DISTINCT ImageUrl FROM ProductVariants WHERE ImageUrl IN @Urls",
                    new { Urls = distinctFiles });

                var filesToDelete = distinctFiles.Except(usedImages).ToList();
                var webRootPath = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        var fullPath = Path.Combine(webRootPath, file.TrimStart('/'));
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            _logger.LogInformation("Deleted orphan image: {Path}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete orphan image: {Path}", file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image cleanup batch");
            }
        }
    }
}
