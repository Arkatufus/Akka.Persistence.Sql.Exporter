// -----------------------------------------------------------------------
// <copyright file="DbUtils.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
// -----------------------------------------------------------------------

using Npgsql;

namespace PostgreSql.Exporter;

public static class DbUtils
{
    public static async Task Initialize(string connectionString)
    {
        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        var databaseName = connectionBuilder.Database;
        if (databaseName is null)
            throw new Exception("Connection string does not contain database name information");
        
        connectionBuilder.Database = "postgres";
        
        await using var conn = new NpgsqlConnection(connectionBuilder.ToString());
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand();
        cmd.CommandText = $@"SELECT TRUE FROM pg_database WHERE datname='{databaseName}'";
        cmd.Connection = conn;
        var result = await cmd.ExecuteScalarAsync();
        
        if (result != null && Convert.ToBoolean(result))
        {
            await CleanAsync(connectionString);
        }
        else
        {
            await DoCreateAsync(conn, databaseName);
        }
    }
    
    public static async Task CleanAsync(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await DoCleanAsync(conn);
    }
    
    private static async Task DoCreateAsync(NpgsqlConnection conn, string databaseName)
    {
        await using var cmd = new NpgsqlCommand();
        cmd.CommandText = $@"CREATE DATABASE {databaseName}";
        cmd.Connection = conn;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DoCleanAsync(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand();
        cmd.CommandText = @"
                    DROP TABLE IF EXISTS public.event_journal;
                    DROP TABLE IF EXISTS public.snapshot_store;
                    DROP TABLE IF EXISTS public.metadata;";
        cmd.Connection = conn;
        await cmd.ExecuteNonQueryAsync();
    }
}