using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Persistence;

namespace oniDash.Music.Actions;

public sealed record MusicFileLocation(Guid FileId, Guid SourceId, string RootPath, string RelativePath, string AbsolutePath);

/// <summary>Performs explicit, validated music file operations and reconciles the shared file index.</summary>
public sealed class MusicFileService(MusicDbContext db)
{
    public async Task<MusicFileLocation?> LocateAsync(Guid fileId, CancellationToken ct)
    {
        const string sql = """
            SELECT f.Id AS FileId, f.LibrarySourceId AS SourceId, s.RootPath AS RootPath,
                   f.RelativePath AS RelativePath
            FROM Files f JOIN Sources s ON s.Id = f.LibrarySourceId
            WHERE f.Id = {0}
            LIMIT 1
            """;
        var rows = await db.Database.SqlQueryRaw<FileRow>(sql, fileId.ToString()).ToListAsync(ct);
        var row = rows.SingleOrDefault();
        if (row is null) return null;
        var relative = row.RelativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolute = Path.GetFullPath(Path.Combine(row.RootPath, relative));
        var root = EnsureRoot(row.RootPath);
        if (!IsUnderRoot(root, absolute)) throw new InvalidOperationException("The indexed path escapes its library source root.");
        return new MusicFileLocation(row.FileId, row.SourceId, root, row.RelativePath, absolute);
    }

    public async Task<FileOperationResult> RenameAsync(Guid fileId, string requestedName, CancellationToken ct)
    {
        var location = await LocateAsync(fileId, ct);
        if (location is null) return FileOperationResult.NotFound("File index entry was not found.");
        var name = Path.GetFileName(requestedName.Trim());
        if (string.IsNullOrWhiteSpace(name) || name != requestedName.Trim() || Path.GetInvalidFileNameChars().Any(name.Contains))
            return FileOperationResult.Invalid("Invalid file name.");
        var destination = Path.Combine(Path.GetDirectoryName(location.AbsolutePath)!, Path.GetFileNameWithoutExtension(name) + Path.GetExtension(location.AbsolutePath));
        return await MovePhysicalAndReconcileAsync(location, destination, ct);
    }

    public async Task<FileOperationResult> MoveAsync(Guid fileId, string relativeDirectory, CancellationToken ct)
    {
        var location = await LocateAsync(fileId, ct);
        if (location is null) return FileOperationResult.NotFound("File index entry was not found.");
        var cleanDirectory = relativeDirectory.Replace('\\', '/').Trim('/');
        if (cleanDirectory.Contains("../", StringComparison.Ordinal) || cleanDirectory == "..") return FileOperationResult.Invalid("Destination must remain inside the source root.");
        var destinationDirectory = Path.GetFullPath(Path.Combine(location.RootPath, cleanDirectory.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderRoot(location.RootPath, destinationDirectory)) return FileOperationResult.Invalid("Destination must remain inside the source root.");
        Directory.CreateDirectory(destinationDirectory);
        return await MovePhysicalAndReconcileAsync(location, Path.Combine(destinationDirectory, Path.GetFileName(location.AbsolutePath)), ct);
    }

    public async Task<FileOperationResult> RemoveFromLibraryAsync(Guid fileId, CancellationToken ct)
    {
        var location = await LocateAsync(fileId, ct);
        if (location is null) return FileOperationResult.NotFound("File index entry was not found.");
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET MissingSinceUtc = COALESCE(MissingSinceUtc, CURRENT_TIMESTAMP) WHERE Id = {fileId}", ct);
        return FileOperationResult.Ok(location.AbsolutePath, false);
    }

    public async Task<FileOperationResult> DeleteFromDiskAsync(Guid fileId, bool confirmed, CancellationToken ct)
    {
        if (!confirmed) return FileOperationResult.Invalid("Deleting a file requires explicit confirmation.");
        var location = await LocateAsync(fileId, ct);
        if (location is null) return FileOperationResult.NotFound("File index entry was not found.");
        if (!File.Exists(location.AbsolutePath))
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET MissingSinceUtc = COALESCE(MissingSinceUtc, CURRENT_TIMESTAMP) WHERE Id = {fileId}", ct);
            return FileOperationResult.Ok(location.AbsolutePath, true);
        }
        File.Delete(location.AbsolutePath);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET MissingSinceUtc = COALESCE(MissingSinceUtc, CURRENT_TIMESTAMP) WHERE Id = {fileId}", ct);
        return FileOperationResult.Ok(location.AbsolutePath, true);
    }

    private async Task<FileOperationResult> MovePhysicalAndReconcileAsync(MusicFileLocation source, string destination, CancellationToken ct)
    {
        destination = Path.GetFullPath(destination);
        if (!IsUnderRoot(source.RootPath, destination)) return FileOperationResult.Invalid("Destination must remain inside the source root.");
        if (string.Equals(source.AbsolutePath, destination, StringComparison.OrdinalIgnoreCase)) return FileOperationResult.Ok(destination, false);
        if (File.Exists(destination)) return FileOperationResult.Conflict("Destination already exists.");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Move(source.AbsolutePath, destination);
            var relative = Path.GetRelativePath(source.RootPath, destination).Replace(Path.DirectorySeparatorChar, '/');
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE Files SET RelativePath = {relative}, MissingSinceUtc = NULL WHERE Id = {source.FileId}", ct);
            return FileOperationResult.Ok(destination, true);
        }
        catch
        {
            // If the physical move succeeded but reconciliation was cancelled, keep the
            // catalogue recoverable: the next scan can identify the moved file by identity.
            throw;
        }
    }

    private static string EnsureRoot(string root) => Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    private static bool IsUnderRoot(string root, string path) => path.StartsWith(EnsureRoot(root), StringComparison.OrdinalIgnoreCase) || string.Equals(path, root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    private sealed record FileRow(Guid FileId, Guid SourceId, string RootPath, string RelativePath);
}

public sealed record FileOperationResult(bool Success, bool Changed, string? Path, string? Error, int StatusCode)
{
    public static FileOperationResult Ok(string path, bool changed) => new(true, changed, path, null, 200);
    public static FileOperationResult NotFound(string error) => new(false, false, null, error, 404);
    public static FileOperationResult Conflict(string error) => new(false, false, null, error, 409);
    public static FileOperationResult Invalid(string error) => new(false, false, null, error, 400);
}
