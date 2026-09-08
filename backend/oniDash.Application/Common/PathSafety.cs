using System;
using System.IO;

namespace oniDash.Application.Common;

/// <summary>
/// Resolves scanner-relative paths without allowing traversal or sibling-prefix escapes.
/// Existing reparse points below the configured root are rejected at resolution time.
/// </summary>
public static class PathSafety
{
    public static bool TryResolveChildPath(
        string rootPath,
        string relativePath,
        out string absolutePath)
    {
        absolutePath = string.Empty;
        if (string.IsNullOrWhiteSpace(rootPath) || string.IsNullOrWhiteSpace(relativePath)
            || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        try
        {
            var root = Path.GetFullPath(rootPath);
            var normalizedRelativePath = relativePath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var candidate = Path.GetFullPath(Path.Combine(root, normalizedRelativePath));
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar)
                ? root
                : root + Path.DirectorySeparatorChar;

            if (!candidate.Equals(root, comparison)
                && !candidate.StartsWith(rootPrefix, comparison))
            {
                return false;
            }

            if (ContainsReparsePointBetween(root, candidate))
            {
                return false;
            }

            absolutePath = candidate;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool ContainsReparsePointBetween(string root, string candidate)
    {
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == ".")
        {
            return false;
        }

        var current = root;
        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (!File.Exists(current) && !Directory.Exists(current))
            {
                break;
            }

            if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }
        }

        return false;
    }
}
