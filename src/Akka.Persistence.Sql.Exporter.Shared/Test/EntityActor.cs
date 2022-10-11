// -----------------------------------------------------------------------
//  <copyright file="PersistenceActor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;
using Akka.Persistence.Journal;

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public sealed class ShardedMessage
{
    public ShardedMessage(string entityId, int message)
    {
        EntityId = entityId;
        Message = message;
    }

    public string EntityId { get; }
    
    public int Message { get; }
}

public sealed class MessageExtractor : HashCodeMessageExtractor
{
    // We're only doing 100 entities
    private const int MaxEntities = 100;

    /// <summary>
    /// We only ever run three nodes, so ~10 shards per node
    /// </summary>
    public MessageExtractor() : base(30)
    {
    }

    public override string? EntityId(object message)
        => message switch
        {
            int i => (i % MaxEntities).ToString(),
            string str => (int.Parse(str) % MaxEntities).ToString(),
            ShardedMessage msg => msg.EntityId,
            ShardingEnvelope msg => msg.EntityId,
            _ => null
        };
}

public sealed class EntityActor : ReceivePersistentActor
{
    public static Props Props(string id) => Actor.Props.Create(() => new EntityActor(id));

    private ILoggingAdapter _log;
    private int _total;
    private readonly string[] _tags = DataGenerator.Tags;

    public EntityActor(string persistenceId)
    {
        _log = Context.GetLogger();
        
        PersistenceId = persistenceId;
        
        Command<int>(msg => Persist(msg, i =>
        {
            _total += i;
        }));

        Command<string>(msg => Persist(msg, str =>
        {
            _total += int.Parse(str);
        }));

        Command<ShardedMessage>(msg =>
        {
            object obj = (msg.Message % 4) switch
            {
                0 => msg,
                1 => new Tagged(msg, new[] { _tags[0] }),
                2 => new Tagged(msg, new[] { _tags[0], _tags[1] }),
                _ => new Tagged(msg, new[] { _tags[0], _tags[1], _tags[2] })
            };
            
            if(obj is Tagged tagged)
            {
                Persist(tagged, sm => { _total += ((ShardedMessage)sm.Payload).Message; });
            }
            else
            {
                Persist(msg, sm => { _total += sm.Message; });
            }
        });

        Command<Finish>(_ =>
        {
            Sender.Tell(_total);
        });

        Recover<int>(msg => _total += msg);
        
        Recover<string>(msg => _total += int.Parse(msg));
        
        Recover<ShardedMessage>(msg => _total += msg.Message);
    }

    public override string PersistenceId { get; }

    protected override void PreStart()
    {
        _log.Debug($"EntityActor({PersistenceId}) started");
        base.PreStart();
    }
}