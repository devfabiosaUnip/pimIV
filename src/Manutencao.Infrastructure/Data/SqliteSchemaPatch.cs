using Microsoft.EntityFrameworkCore;

namespace Manutencao.Infrastructure.Data;

public static class SqliteSchemaPatch
{
    public static async Task ApplyAsync(AppDbContext db, CancellationToken ct = default)
    {
        var columns = await GetColumnsAsync(db, "VisitAssignments", ct);
        if (columns.Count > 0)
        {
            if (!columns.Contains("CompletionNotes"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE VisitAssignments ADD COLUMN CompletionNotes TEXT NULL;", ct);
            if (!columns.Contains("CompletionPhotoPath"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE VisitAssignments ADD COLUMN CompletionPhotoPath TEXT NULL;", ct);
            if (!columns.Contains("CompletedAt"))
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE VisitAssignments ADD COLUMN CompletedAt TEXT NULL;", ct);
        }

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ClientFeedbacks (
                Id TEXT NOT NULL PRIMARY KEY,
                ServiceRequestId TEXT NOT NULL,
                Rating INTEGER NOT NULL,
                Comment TEXT NULL,
                CreatedAt TEXT NOT NULL
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_ClientFeedbacks_ServiceRequestId
            ON ClientFeedbacks(ServiceRequestId);
            """, ct);
    }

    private static async Task<HashSet<string>> GetColumnsAsync(AppDbContext db, string table, CancellationToken ct)
    {
        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            cols.Add(reader.GetString(1));
        return cols;
    }
}
