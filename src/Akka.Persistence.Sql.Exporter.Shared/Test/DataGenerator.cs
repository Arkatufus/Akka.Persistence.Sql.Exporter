// -----------------------------------------------------------------------
//  <copyright file="DataGenerator.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Persistence.Sql.Compat.Common;

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
        await GenerateDataAsync(region, token);
        await UntilSnapshotAndClearCompleteAsync(region, token);
        await GenerateDataAsync(region, token);
        await UntilSnapshotCompleteAsync(region, token);
        await GenerateDataAsync(region, token);
        await UntilFinishedAsync(region, token);
    }

    private static async Task GenerateDataAsync(IActorRef region, CancellationToken token)
    {
        const int pauseEvery = 500;
        const int pauseMillis = 500;
        var count = 0;
        
        // 4 test types: int, string, ShardedMessage, and CustomShardedMessage
        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(i);

            count++;
            if (count % pauseEvery == 0)
            {
                Console.WriteLine($"Sent {count} data, pausing for {pauseMillis} ms.");
                await Task.Delay(pauseMillis, token);
            }
            if (token.IsCancellationRequested)
                return;
        }

        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(i.ToString());

            count++;
            if (count % pauseEvery == 0)
            {
                Console.WriteLine($"Sent {count} data, pausing for {pauseMillis} ms.");
                await Task.Delay(pauseMillis, token);
            }
            if (token.IsCancellationRequested)
                return;
        }
        
        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(new ShardedMessage(i));

            count++;
            if (count % pauseEvery == 0)
            {
                Console.WriteLine($"Sent {count} data, pausing for {pauseMillis} ms.");
                await Task.Delay(pauseMillis, token);
            }
            if (token.IsCancellationRequested)
                return;
        }
        
        foreach (var i in Enumerable.Range(0, Utils.MessagesPerType))
        {
            region.Tell(new CustomShardedMessage(i));

            count++;
            if (count % pauseEvery == 0)
            {
                Console.WriteLine($"Sent {count} data, pausing for {pauseMillis} ms.");
                await Task.Delay(pauseMillis, token);
            }
            if (token.IsCancellationRequested)
                return;
        }
        
    }

    private static async Task UntilFinishedAsync(IActorRef region, CancellationToken token)
    {
        var tasks = Enumerable.Range(0, 100).Select(id => region.Ask<(string, StateSnapshot, int, int)>(new Finish(id), token)).ToList();
        while (tasks.Count > 0)
        {
            var task = await Task.WhenAny(tasks);
            if (token.IsCancellationRequested)
                return;
            
            tasks.Remove(task);
            var (id, lastSnapshot, total, persisted) = task.Result;
            Console.WriteLine(
                $"{id} data received. " +
                $"Snapshot: [Total: {lastSnapshot.Total}, Persisted: {lastSnapshot.Persisted}] " +
                $"State: [Total: {total}, Persisted: {persisted}]");
        }
    }

    private static async Task UntilSnapshotCompleteAsync(IActorRef region, CancellationToken token)
    {
        var tasks = Enumerable.Range(0, 100).Select(id => region.Ask<(string, StateSnapshot)>(new TakeSnapshot(id), token)).ToList();
        while (tasks.Count > 0)
        {
            var task = await Task.WhenAny(tasks);
            if (token.IsCancellationRequested)
                return;
            
            tasks.Remove(task);
            var (id, lastSnapshot) = task.Result;
            Console.WriteLine(
                $"{id} snapshot completed. " +
                $"Snapshot: [Total: {lastSnapshot.Total}, Persisted: {lastSnapshot.Persisted}]");
        }
    }

    private static async Task UntilSnapshotAndClearCompleteAsync(IActorRef region, CancellationToken token)
    {
        var tasks = Enumerable.Range(0, 100).Select(id => region.Ask<(string, StateSnapshot)>(new TakeSnapshotAndClear(id), token)).ToList();
        while (tasks.Count > 0)
        {
            var task = await Task.WhenAny(tasks);
            if (token.IsCancellationRequested)
                return;
            
            tasks.Remove(task);
            var (id, lastSnapshot) = task.Result;
            Console.WriteLine(
                $"{id} snapshot completed, journal cleared. " +
                $"Snapshot: [Total: {lastSnapshot.Total}, Persisted: {lastSnapshot.Persisted}]");
        }
    }
}