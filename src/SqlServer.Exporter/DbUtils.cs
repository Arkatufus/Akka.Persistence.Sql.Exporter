// -----------------------------------------------------------------------
// <copyright file="DbUtils.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

namespace SqlServer.Exporter;

public static class DbUtils
{
    public static async Task Initialize(string connectionString)
    {
        var connectionBuilder = new SqlConnectionStringBuilder(connectionString);

        var databaseName = connectionBuilder.InitialCatalog;
        connectionBuilder.InitialCatalog = "master";
        var newConnStr = connectionBuilder.ToString();

        await using var conn = new SqlConnection(newConnStr);
        conn.Open();

        await using var cmd = new SqlCommand();
        cmd.CommandText = @$"
IF db_id('{databaseName}') IS NULL
    BEGIN
        CREATE DATABASE {databaseName}
    END
";
        cmd.Connection = conn;

        await cmd.ExecuteScalarAsync();
        await DropTablesAsync(conn, databaseName);
    }

    public static async Task CleanAsync(string connectionString)
    {
        var connectionBuilder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = connectionBuilder.InitialCatalog;

        await using var conn = new SqlConnection(connectionString);
        conn.Open();
        await DropTablesAsync(conn, databaseName);
    }

    private static async Task DropTablesAsync(SqlConnection conn, string databaseName)
    {
        await using var cmd = new SqlCommand();
        cmd.CommandText = $@"
                    USE {databaseName};
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'EventJournal') BEGIN DROP TABLE dbo.EventJournal END;
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Metadata') BEGIN DROP TABLE dbo.Metadata END;
                    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SnapshotStore') BEGIN DROP TABLE dbo.SnapshotStore END;";
        cmd.Connection = conn;
        await cmd.ExecuteNonQueryAsync();
    }
}