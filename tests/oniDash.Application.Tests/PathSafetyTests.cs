using System;
using System.IO;
using oniDash.Application.Common;
using Xunit;

namespace oniDash.Application.Tests;

public sealed class PathSafetyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "onidash-path-tests", Guid.NewGuid().ToString("N"));

    public PathSafetyTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Resolves_a_normal_child_path()
    {
        var ok = PathSafety.TryResolveChildPath(_root, "movies/film.mkv", out var path);

        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(Path.Combine(_root, "movies", "film.mkv")), path);
    }

    [Theory]
    [InlineData("../outside.mkv")]
    [InlineData("../../outside.mkv")]
    public void Rejects_parent_traversal(string relativePath)
    {
        Assert.False(PathSafety.TryResolveChildPath(_root, relativePath, out _));
    }

    [Fact]
    public void Rejects_a_sibling_prefix_escape()
    {
        var sibling = _root + "-outside" + Path.DirectorySeparatorChar + "film.mkv";

        Assert.False(PathSafety.TryResolveChildPath(_root, Path.GetRelativePath(_root, sibling), out _));
    }

    [Fact]
    public void Rejects_rooted_paths()
    {
        var rooted = Path.Combine(Path.GetTempPath(), "outside.mkv");

        Assert.False(PathSafety.TryResolveChildPath(_root, rooted, out _));
    }

    [Fact]
    public void Rejects_existing_reparse_point_below_root_when_supported()
    {
        var link = Path.Combine(_root, "linked");
        var target = Path.Combine(_root, "target");
        Directory.CreateDirectory(target);

        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        catch (PlatformNotSupportedException)
        {
            return;
        }
        catch (IOException)
        {
            return;
        }

        Assert.False(PathSafety.TryResolveChildPath(_root, "linked/film.mkv", out _));
    }
}
