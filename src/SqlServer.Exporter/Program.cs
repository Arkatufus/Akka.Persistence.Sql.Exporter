// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

await using var docker = new SqlServerDocker();
docker.OnStdOut += (_, outputArgs) =>
{
    Console.WriteLine(outputArgs.Output);
};
await docker.StartAsync();

var config = ConfigurationFactory.ParseString($@"
    akka.loglevel = DEBUG
    akka.persistence.journal {{
        plugin = ""akka.persistence.journal.sql-server""
        sql-server {{
            auto-initialize = on
            connection-string = ""{docker.ConnectionString}""
        }}
    }}
    akka.persistence.snapshot-store {{
        plugin = ""akka.persistence.snapshot-store.sql-server""
        sql-server {{
            auto-initialize = on
            connection-string = ""{docker.ConnectionString}""
        }}
    }}").WithFallback(SqlServerPersistence.DefaultConfiguration());

using var sys = ActorSystem.Create("actorSystem", config);

await sys.CreateTestData();

Console.WriteLine(">>>>>>>>>>> downloading backup");

await docker.DownloadAsync("/var/opt/mssql/data/", docker.OutputPath, "data.tar");

Console.WriteLine(">>>>>>>>>>> DONE!");
