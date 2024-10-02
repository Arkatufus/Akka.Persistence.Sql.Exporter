# Akka.Persistence.Sql.Exporter

This repository is used to generate data for `Akka.Persistence.Sql` backward compatibility test. It generates a standardized test data and package them inside a docker container.

## Supported Akka Persistence Module

* Akka.Persistence.MySql
* Akka.Persistence.PostgreSql
* Akka.Persistence.Sqlite
* Akka.Persistence SqlServer

## Data Generation

* Persisted data types: `int`, `string`, `ShardedMessage`, `CustomShardedMessage`
  * `ShardedMessage` contains a single int payload and saved using standard serializer
  * `CustomShardedMessage` contains a single int payload and saved using a custom serializer `CustomSerializer` with serializer ID `999`
* Each data types are persisted using 0, 1, and 2 tags
  * Tags are "Tag1" and "Tag2"
* Data are generated using a 3 node cluster into 100 entities.

### Entity State

The test entities are very simple, they contain 2 tracked states:
* __Total__: The aggregate total of all passed messages.
* __Persisted__: The total number of messages received by the entity.

### Entity ID Generation

Entity ID are generated from the int message itself using this formula:

```csharp
int msg;
const int MaxEntities = 100;
string entityId = ((msg / 3) % MaxEntities).ToString();
```

### Round Of Data Generation

A round of data generation is done by sending a sequence of [0..299] integer messages of each data type to the shard region actor. On each round, each entity will persist exactly 12 data consisting each data type in all the tag variants.

### Full Data Generation

A full data generation is done in this exact order:

* Round of data generation.
* Save snapshot and delete journal.
* Round of data generation.
* Save snapshot.
* Round of data generation.

At the end of data generation, each entity should have persisted 36 messages and have 2 snapshots.

### Database Content

At the end of full data generation, the database journal, snapshot, and metadata table will contain data from both Sharding and Persistence.

### Predicting Final Entity State

#### State

```csharp
var baseValue = persistentId * 3;
var roundTotal = (baseValue + baseValue + 1 + baseValue + 2) * 4;

Total = roundTotal * 3;
Persisted = 36;
``` 

#### Tags

```csharp
int msg;
var tagCount = (msg % 3);
```

* The message is not tagged tagged if `msg % 3 == 0`,
* tagged with `["Tag1"]` if `msg % 3 == 1`, and
* tagged with `["Tag1", "Tag2"]` when `msg % 3 == 2`.

## Creating Test Environment

All the needed environment code are in the `Akka.Persistence.Sql.Exporter.Shared` project. The start code automatically start a 3 node cluster with all the required configuration set.

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
