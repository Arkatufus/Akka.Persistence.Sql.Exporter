// -----------------------------------------------------------------------
//  <copyright file="SqlServerDocker.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using Akka.Util;
using Docker.DotNet.Models;

namespace SqlServer.Exporter;

public class SqlServerDocker: DockerContainer
{
    public SqlServerDocker() : base("mcr.microsoft.com/mssql/server", "2019-latest", $"mssql-{Guid.NewGuid():N}")
    {
        
    }

    public string? ConnectionString { get; private set; }
        
    private int Port { get; } = ThreadLocalRandom.Current.Next(9000, 10000);
    
    private string User { get; } = "sa";
    
    private string Password { get; } = "Password12!";
    
    protected override string ReadyMarker => "Recovery is complete.";
    
    protected override void ConfigureContainer(CreateContainerParameters parameters)
    {
        parameters.ExposedPorts = new Dictionary<string, EmptyStruct>
        {
            ["1433/tcp"] = new()
        };
        parameters.HostConfig = new HostConfig
        {
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                ["1433/tcp"] = new List<PortBinding> { new() { HostPort = $"{Port}" } }
            }
        };
        parameters.Env = new[]
        {
            "ACCEPT_EULA=Y",
            $"MSSQL_SA_PASSWORD={Password}",
            "MSSQL_PID=Express"
        };
    }
    
    protected override async Task AfterContainerStartedAsync(CancellationToken cancellationToken)
    {
        var builder = new DbConnectionStringBuilder
        {
            ["Server"] = $"localhost,{Port}",
            ["Database"] = DatabaseName,
            ["User Id"] = User,
            ["Password"] = Password
        };

        ConnectionString = builder.ToString();
        await DbUtils.Initialize(ConnectionString);
        
        Console.WriteLine($"Connection string: [{ConnectionString}]");
    }
}