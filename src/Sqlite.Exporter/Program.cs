// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Hosting;
using Akka.Persistence.Sql.Exporter.Shared.Test;

var outputPath = Env.OutputPath;
if (!Directory.Exists(outputPath))
    Directory.CreateDirectory(outputPath);

var dbFile = Path.Combine(outputPath, "database.db");
if (!File.Exists(dbFile))
    File.Create(dbFile).Close();

var uri = new Uri($"file://{dbFile.Replace("\\", "/")}");
var connectionString = $"Filename={uri}";

void Setup(AkkaConfigurationBuilder builder, IServiceProvider provider)
{
    var config = ConfigurationFactory.ParseString($@"
    akka.loglevel = DEBUG
    akka.persistence.journal {{
        plugin = ""akka.persistence.journal.sqlite""
        sqlite {{
            auto-initialize = on
            connection-string = ""{connectionString}""
        }}
    }}
    akka.persistence.snapshot-store {{
        plugin = ""akka.persistence.snapshot-store.sqlite""
        sqlite {{
            auto-initialize = on
            connection-string = ""{connectionString}""
        }}
    }}").WithFallback(SqlitePersistence.DefaultConfiguration());

    builder
        .AddHocon(config);
}

await using var testCluster = new TestCluster(Setup);
await testCluster.StartAsync();

var generator = new DataGenerator(testCluster);
await generator.GenerateAsync();

Console.WriteLine(">>>>>>>>>>> DONE!");
