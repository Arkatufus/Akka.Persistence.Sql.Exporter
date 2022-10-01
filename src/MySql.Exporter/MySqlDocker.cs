// -----------------------------------------------------------------------
//  <copyright file="MySqlDocker.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace MySql.Exporter;

/// <summary>
///     Fixture used to run SQL Server
/// </summary>
public class MySqlDocker : DockerContainer
{
    public MySqlDocker() : base("mysql", "8", $"mysql-{Guid.NewGuid():N}")
    {
    }

    public string? ConnectionString { get; private set; }
        
    private int Port { get; } = ThreadLocalRandom.Current.Next(9000, 10000);
    private string User { get; } = "root";
    private string Password { get; } = "Password12!";
        
    protected override string ReadyMarker => "ready for connections.";

    protected override void ConfigureContainer(CreateContainerParameters parameters)
    {
        parameters.ExposedPorts = new Dictionary<string, EmptyStruct>
        {
            ["3306/tcp"] = new()
        };
        parameters.HostConfig = new HostConfig
        {
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                ["3306/tcp"] = new List<PortBinding> { new() { HostPort = $"{Port}" } }
            }
        };
        parameters.Env = new[]
        {
            $"MYSQL_ROOT_PASSWORD={Password}",
            $"MYSQL_DATABASE={DatabaseName}",
        };
    }

    public async Task DumpDatabase(string outputFile)
    {
        try
        {
            await ExecuteCommandAsync("sh", "-c",  $"exec mysqldump -u{User} -p'{Password}' {DatabaseName} > {outputFile}");
        }
        catch (Exception e)
        {
            Console.WriteLine($">>>>>>>>>> Failed to execute command. {e}");
        }
    }

    protected override Task AfterContainerStartedAsync(CancellationToken cancellationToken)
    {
        var connectionString = new DbConnectionStringBuilder
        {
            ["Server"] = "localhost",
            ["Port"] = Port.ToString(),
            ["Database"] = DatabaseName,
            ["User Id"] = User,
            ["Password"] = Password
        };

        ConnectionString = connectionString.ToString();
        Console.WriteLine($"Connection string: [{ConnectionString}]");
            
        return Task.CompletedTask;
    }

}