using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using oniDash.Music.Persistence;

namespace oniDash.Music.Tests;

/// <summary>Shared in-memory MusicDbContext with a minimal core-schema shim.</summary>
public sealed class MusicTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public MusicTestDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:;Foreign Keys=True");
        _connection.Open();
        Options = new DbContextOptionsBuilder<MusicDbContext>().UseSqlite(_connection).Options;
        CreateCoreSchemaShim();
        using var context = new MusicDbContext(Options);
        context.Database.Migrate();
    }

    public DbContextOptions<MusicDbContext> Options { get; }

    public MusicDbContext CreateContext() => new(Options);

    public void Dispose() => _connection.Dispose();

    private void CreateCoreSchemaShim()
    {
        using var command = _connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS "Libraries" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "Sources" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "LibraryId" TEXT NOT NULL,
                "Name" TEXT NOT NULL,
                "RootPath" TEXT NOT NULL,
                "LastScannedAtUtc" TEXT,
                "CreatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "MediaItems" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "LibraryId" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "CreatedAtUtc" TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS "Files" (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "MediaItemId" TEXT NOT NULL,
                "LibrarySourceId" TEXT NOT NULL,
                "RelativePath" TEXT NOT NULL,
                "IdentityKey" TEXT NOT NULL,
                "Extension" TEXT NOT NULL,
                "SizeBytes" INTEGER NOT NULL,
                "LastWriteTimeUtc" TEXT NOT NULL,
                "MissingSinceUtc" TEXT,
                "CreatedAtUtc" TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
