using System;
using System.IO;

namespace PCS_API.Services
{
    public static class AttachmentHelper
    {
        public static string? CommitAttachment(string? url, string webRootPath, string subFolder = "transactions")
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            // Check if the URL path points to the temp folder
            const string tempPrefix = "/uploads/temp/";
            if (!url.StartsWith(tempPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return url; // Already committed or external
            }

            try
            {
                var fileName = Path.GetFileName(url);
                var tempFilePath = Path.Combine(webRootPath, "uploads", "temp", fileName);
                
                if (File.Exists(tempFilePath))
                {
                    var targetDir = Path.Combine(webRootPath, "uploads", subFolder);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    var targetFilePath = Path.Combine(targetDir, fileName);
                    
                    // If the file already exists in the target directory (e.g., due to duplicate hash),
                    // we can safely delete it from the temp directory.
                    if (File.Exists(targetFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                    else
                    {
                        File.Move(tempFilePath, targetFilePath);
                    }

                    return $"/uploads/{subFolder}/{fileName}";
                }
            }
            catch (Exception)
            {
                // Fallback to original URL on failure so that business logic doesn't crash completely.
            }

            return url;
        }
    }
}
