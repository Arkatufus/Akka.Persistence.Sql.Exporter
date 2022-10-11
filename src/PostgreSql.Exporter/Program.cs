// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------


await using var docker = new PostgreSqlDocker();
docker.OnStdOut += (_, outputArgs) =>
{
    Console.WriteLine(outputArgs.Output);
};
await docker.StartAsync();

void Setup(AkkaConfigurationBuilder builder, IServiceProvider provider)
{
    builder
        .WithPostgreSqlPersistence(docker.ConnectionString, autoInitialize:true);
}

await using var testCluster = new TestCluster(Setup);
await testCluster.StartAsync();

var generator = new DataGenerator(testCluster);
await generator.GenerateAsync();

Console.WriteLine(">>>>>>>>>>> Creating backup");
await docker.DumpDatabaseAsync("backup.sql");

Console.WriteLine(">>>>>>>>>>> downloading backup");
await docker.DownloadAsync("backup.sql", docker.OutputPath, "backup.tar", true, false);

Console.WriteLine(">>>>>>>>>>> DONE!");