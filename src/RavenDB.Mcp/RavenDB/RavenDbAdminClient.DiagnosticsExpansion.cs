using System.Text;
using System.Text.Json;
using ModelContextProtocol;
using RavenDB.Mcp.Tools;

namespace RavenDB.Mcp.RavenDB;

public sealed partial class RavenDbAdminClient
{
    private async Task<JsonElement> TryGetServerJson(
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        try
        {
            return ToJson(new
            {
                available = true,
                value = await GetServerJson(path, cancellationToken, query)
            });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ToJson(new
            {
                available = false,
                error = exception.Message
            });
        }
    }

    private async Task<JsonElement> TryGetDatabaseJson(
        string databaseName,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        try
        {
            return ToJson(new
            {
                available = true,
                value = await GetDatabaseJson(databaseName, path, cancellationToken, query)
            });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ToJson(new
            {
                available = false,
                error = exception.Message
            });
        }
    }

    private async Task<JsonElement> TryReadJson<T>(Func<Task<T>> read, CancellationToken cancellationToken)
    {
        try
        {
            return ToJson(new { available = true, value = await read() });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ToJson(new { available = false, error = exception.Message });
        }
    }

    private async Task<JsonElement> PostDatabaseJson(
        string databaseName,
        string path,
        object body,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        ValidateDatabaseName(databaseName);

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(BuildDatabaseUrl(databaseName, path, query), content, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new RavenRequestException((int)response.StatusCode, responseText,
                $"POST {path} failed with {(int)response.StatusCode}: {responseText}");

        return JsonSerializer.Deserialize<JsonElement>(responseText);
    }

    private async Task<DiagnosticArtifactResult> SaveServerArtifact(
        string name,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        return await SaveArtifact(name, await GetBytes(BuildServerUrl(path, query), cancellationToken), cancellationToken);
    }

    private async Task<DiagnosticArtifactResult> SaveDatabaseArtifact(
        string databaseName,
        string name,
        string path,
        CancellationToken cancellationToken,
        params (string Name, string? Value)[] query)
    {
        return await SaveArtifact(
            $"{databaseName}-{name}",
            await GetBytes(BuildDatabaseUrl(databaseName, path, query), cancellationToken),
            cancellationToken);
    }

    private async Task<(byte[] Bytes, string ContentType)> GetBytes(string url, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new McpException($"GET {url} failed with {(int)response.StatusCode}: {Encoding.UTF8.GetString(bytes)}");

        return (bytes, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    // Exported artifacts can outlive their usefulness by weeks. When we own the default location we
    // delete our own leftovers older than this on the next export, so secrets in old debug packages
    // do not linger in a temp folder indefinitely.
    private static readonly TimeSpan ArtifactRetention = TimeSpan.FromHours(24);

    private async Task<DiagnosticArtifactResult> SaveArtifact(
        string name,
        (byte[] Bytes, string ContentType) content,
        CancellationToken cancellationToken)
    {
        EnsureArtifactsDirectory();

        if (artifactsPathIsDefault)
            CleanupExpiredArtifacts();

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{SanitizeFileName(name)}{ExtensionFor(content.ContentType)}";
        var path = Path.Combine(artifactsPath, fileName);
        await File.WriteAllBytesAsync(path, content.Bytes, cancellationToken);

        return new DiagnosticArtifactResult(path, content.ContentType, content.Bytes.LongLength);
    }

    private void EnsureArtifactsDirectory()
    {
        Directory.CreateDirectory(artifactsPath);

        // The default location is a shared temp dir (world-readable /tmp on Linux). Exported packages
        // and log dumps can contain secrets that structural redaction cannot mask, so restrict the
        // folder to the current user. Best-effort: some filesystems reject chmod, and the export
        // must still succeed. A user-supplied ArtifactsPath is left exactly as configured.
        if (artifactsPathIsDefault && !OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(
                    artifactsPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
            }
        }
    }

    private void CleanupExpiredArtifacts()
    {
        var cutoff = DateTime.UtcNow - ArtifactRetention;

        try
        {
            foreach (var file in Directory.EnumerateFiles(artifactsPath))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                        File.Delete(file);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // A file we cannot delete (in use, permissions) is skipped, not fatal.
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            // Cleanup is best-effort; never let it break an export.
        }
    }

    // Name the artifact for what it actually is, so the returned path is directly openable.
    private static string ExtensionFor(string contentType) => contentType switch
    {
        "application/zip" => ".zip",
        "application/json" => ".json",
        "text/plain" => ".txt",
        _ => ".bin",
    };

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
            builder.Append(invalid.Contains(character) ? '-' : character);

        return builder.ToString();
    }
}
