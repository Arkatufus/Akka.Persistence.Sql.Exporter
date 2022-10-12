// -----------------------------------------------------------------------
//  <copyright file="DataGenerator.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

using Akka.Actor;

public sealed class DataGenerator 
{
    private readonly TestCluster _testCluster;

    public DataGenerator(TestCluster testCluster)
    {
        _testCluster = testCluster;
    }

    public async Task GenerateAsync(CancellationToken token = default)
    {
        if (!_testCluster.IsStarted)
            throw new Exception("Test cluster has not been started yet.");

        var region = _testCluster.ShardRegions.First();

        // 4 test types: int, string, ShardedMessage, and CustomShardedMessage
        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(i);
        }

        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(i.ToString());
        }
        
        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(new ShardedMessage(i));
        }
        
        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(new CustomShardedMessage(i));
        }
        
        var tasks = Enumerable.Range(0, 100).Select(id => region.Ask<(string, int)>(new Finish(id))).ToList();
        while (tasks.Count > 0)
        {
            var task = await Task.WhenAny(tasks);
            tasks.Remove(task);
            var (id, count) = task.Result;
            Console.WriteLine($">>>>> Entity {id} persisted {count} items");
        }
    }

}