namespace ShramSetu.Services;

public interface IFileStorageService
{
    /// <summary>Saves an uploaded file and returns its public URL path.</summary>
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default);

    /// <summary>Deletes a file given its relative URL path.</summary>
    Task DeleteAsync(string fileUrl, CancellationToken ct = default);

    /// <summary>Returns true if the file extension is allowed.</summary>
    bool IsAllowed(IFormFile file, string[] allowedExtensions);
}

/// <summary>
/// Local disk storage  saves files to wwwroot/uploads/{folder}/.
/// Swap this for AzureBlobStorageService in production.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;

    // Max 5 MB per file
    private const long MaxFileSize = 5 * 1024 * 1024;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env    = env;
        _logger = logger;
    }

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file.Length > MaxFileSize)
            throw new InvalidOperationException($"File size exceeds {MaxFileSize / 1024 / 1024} MB limit.");

        var uploadRoot = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadRoot);

        var ext      = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadRoot, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        var url = $"/uploads/{folder}/{fileName}";
        _logger.LogInformation("File saved: {Url}", url);
        return url;
    }

    public Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return Task.CompletedTask;

        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath     = Path.Combine(_env.WebRootPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {Path}", fileUrl);
        }
        return Task.CompletedTask;
    }

    public bool IsAllowed(IFormFile file, string[] allowedExtensions)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }
}
