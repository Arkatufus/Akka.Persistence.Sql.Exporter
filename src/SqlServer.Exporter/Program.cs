// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Hosting;
using Akka.Persistence.Sql.Exporter.Shared.Test;
using Akka.Persistence.SqlServer.Hosting;

await using var docker = new SqlServerDocker();
docker.OnStdOut += (_, outputArgs) =>
{
    Console.WriteLine(outputArgs.Output);
};
await docker.StartAsync();

void Setup(AkkaConfigurationBuilder builder, IServiceProvider provider)
{
    builder
        .WithSqlServerPersistence(docker.ConnectionString, autoInitialize:true);
}

await using var testCluster = new TestCluster(Setup, "sql-server");
await testCluster.StartAsync();

var generator = new DataGenerator(testCluster);
await generator.GenerateAsync();

Console.WriteLine(">>>>>>>>>>> downloading backup");

await docker.DownloadAsync("/var/opt/mssql/data/", docker.OutputPath, "data.tar");

Console.WriteLine(">>>>>>>>>>> DONE!");
