// -----------------------------------------------------------------------
//  <copyright file="Messages.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public interface IHasEntityId
{
    public string EntityId { get; }
}

public sealed class Finish: IHasEntityId
{
    public Finish(int entityId)
    {
        EntityId = (entityId % Utils.MaxEntities).ToString();
    }

    public string EntityId { get; }
}

public sealed class TakeSnapshotAndClear: IHasEntityId
{
    public TakeSnapshotAndClear(int entityId)
    {
        EntityId = (entityId % Utils.MaxEntities).ToString();
    }

    public string EntityId { get; }
}

public sealed class TakeSnapshot: IHasEntityId
{
    public TakeSnapshot(int entityId)
    {
        EntityId = (entityId % Utils.MaxEntities).ToString();
    }

    public string EntityId { get; }
}

public sealed class ShardedMessage: IHasEntityId
{
    public ShardedMessage(int message)
    {
        EntityId = message.ToEntityId();
        Message = message;
    }

    public string EntityId { get; }
    
    public int Message { get; }
}

public sealed class CustomShardedMessage: IHasEntityId
{
    public CustomShardedMessage(int message)
    {
        EntityId = message.ToEntityId();
        Message = message;
    }

    public string EntityId { get; }
    
    public int Message { get; }
}

public sealed class StateSnapshot
{
    public static readonly StateSnapshot Empty = new StateSnapshot(0, 0);
    
    public StateSnapshot(int total, int persisted)
    {
        Total = total;
        Persisted = persisted;
    }

    public int Total { get; }
    public int Persisted { get; }
}
