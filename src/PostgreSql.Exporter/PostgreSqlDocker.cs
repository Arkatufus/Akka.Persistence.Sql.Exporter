// -----------------------------------------------------------------------
//  <copyright file="SqlServerDocker.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace PostgreSql.Exporter;

public class PostgreSqlDocker: DockerContainer
{
    //public PostgreSqlDocker() : base("postgres", "14-alpine", $"postgresSqlServer-{Guid.NewGuid():N}")
    public PostgreSqlDocker() : base("postgres", "14-alpine", $"postgres")
    {
    }

    public string ConnectionString { get; private set; } = "";
    
    private int Port { get; } = ThreadLocalRandom.Current.Next(9000, 10000);
    
    private string User { get; } = "postgres";
    
    private string Password { get; } = "postgres";

    protected override string ReadyMarker => "ready to accept connections";
    protected override int ReadyCount => 2;

    protected override void ConfigureContainer(CreateContainerParameters parameters)
    {
        parameters.ExposedPorts = new Dictionary<string, EmptyStruct>
        {
            {"5432/tcp", new ()}
        };
        parameters.HostConfig = new HostConfig
        {
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                ["5432/tcp"] = new List<PortBinding> { new() { HostPort = $"{Port}" } }
            }
        };
        parameters.Env = new[]
        {
            $"POSTGRES_PASSWORD={Password}",
            $"POSTGRES_USER={User}",
            "PGDATA=/data"
        };
    }
    
    protected override async Task AfterContainerStartedAsync(CancellationToken cancellationToken)
    {
        var builder = new DbConnectionStringBuilder
        {
            ["Server"] = "localhost",
            ["Port"] = Port,
            ["Database"] = DatabaseName,
            ["User Id"] = User,
            ["Password"] = Password
        };

        ConnectionString = builder.ToString();
        await DbUtils.Initialize(ConnectionString);
        
        Console.WriteLine($"Connection string: [{ConnectionString}]");
    }
    
    public async Task DumpDatabaseAsync(string outputFile)
    {
        try
        {
            await ExecuteCommandAsync("pg_dump", "-h", "localhost", "-U", User, "-C", $"--file={outputFile}", DatabaseName);
        }
        catch (Exception e)
        {
            Console.WriteLine($">>>>>>>>>> Failed to execute command. {e}");
        }
    }
}