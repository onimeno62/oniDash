using System.IO;
using oniDash.Application.Libraries;

namespace oniDash.Infrastructure.Repositories;

/// <summary>Local filesystem existence checks for the application layer.</summary>
public sealed class FileSystemProbe : IFileSystemProbe
{
    public bool IsExistingDirectory(string path) => Directory.Exists(path);
}
