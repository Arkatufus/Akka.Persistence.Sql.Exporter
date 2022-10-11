// -----------------------------------------------------------------------
//  <copyright file="DataGenerator.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public sealed class DataGenerator 
{
    public static readonly string[] Tags = { "Tag1", "Tag2", "Tag3", "Tag4" };

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
        
    }

}