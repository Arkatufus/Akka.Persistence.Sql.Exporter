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
        EntityId = (entityId % Const.MaxEntities).ToString();
    }

    public string EntityId { get; }
}

public sealed class ShardedMessage: IHasEntityId
{
    public ShardedMessage(int message)
    {
        EntityId = (message % Const.MaxEntities).ToString();
        Message = message;
    }

    public string EntityId { get; }
    
    public int Message { get; }
}

public sealed class AdaptedShardedMessage: IHasEntityId
{
    public AdaptedShardedMessage(int message)
    {
        EntityId = (message % Const.MaxEntities).ToString();
        Message = message;
    }

    public string EntityId { get; }
    
    public int Message { get; }
}
