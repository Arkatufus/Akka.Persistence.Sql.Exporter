# Akka.Persistence.Sql.Exporter

This repository is used to generate data for `Akka.Persistence.Sql` backward compatibility test. It generate a standardized test data and package them inside a docker container.

## Supported Akka Persistence Module

* Akka.Persistence.MySql
* Akka.Persistence.PostgreSql
* Akka.Persistence.Sqlite
* Akka.Persistence SqlServer

## Data Generation

* Persisted data types: `int`, `string`, `ShardedMessage`, `CustomShardedMessage`
  * `ShardedMessage` is saved using standard serializer
  * `CustomShardedMessage` is saved using a custom serializer `CustomSerializer`
* Each data types are persisted using 0, 1, and 2 tags
  * Tags are "Tag1" and "Tag2"
* Data are generated using a 3 node cluster into 100 entities.
* Each entity will persist exactly 12 data consisting each data type in all of the tag variants

## Creating Test Environment

All of the needed environment code are in the `Akka.Persistence.Sql.Exporter.Shared` project. The start code automatically start a 3 node cluster with all of the required configuration set.

```csharp
void Setup(AkkaConfigurationBuilder builder, IServiceProvider provider)
{
    var config = ConfigurationFactory.ParseString($@"
                akka.persistence.journal {{
                    plugin = ""akka.persistence.journal.mysql""
                    mysql.connection-string = ""{docker.ConnectionString}""
                }}
                akka.persistence.snapshot-store {{
                    plugin = ""akka.persistence.snapshot-store.mysql""
                    mysql.connection-string = ""{docker.ConnectionString}""
                }}").WithFallback(MySqlPersistence.DefaultConfiguration());

    builder
        .AddHocon(config);
}

await using var testCluster = new TestCluster(Setup, "mysql");
await testCluster.StartAsync();
```

## Building Docker Image 

* Change the plugin version in the .csproj file to the version you want to make a specific data dump for
* Run the `build.ps1` script
  * `.\build.ps1 all` to create all data docker images
  * `.\build.ps1 mysql` to create a MySql docker image
  * `.\build.ps1 postgresql` to create a PostgreSql docker image
  * `.\build.ps1 sqlite` to create a SqLite data file
  * `.\build.ps1 sqlserver` to create a MS SQL Server docker image
