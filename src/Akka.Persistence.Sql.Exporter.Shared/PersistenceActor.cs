// -----------------------------------------------------------------------
//  <copyright file="PersistenceActor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.Persistence.Sql.Exporter.Shared;

public class PersistenceActor: PersistentActor
{
    public static Props Props(string id) => Actor.Props.Create(() => new PersistenceActor(id));

    private long _count;
    private long _state;
    
    public PersistenceActor(string id)
    {
        PersistenceId = id;
    }
    
    protected override bool ReceiveRecover(object message)
    {
        switch (message)
        {
            case Stored s:
                _count++;
                _state += s.Value;
                return true;
            default:
                return false;
        }
    }

    protected override bool ReceiveCommand(object message)
    {
        switch (message)
        {
            case Store s:
                Persist(new Stored(s.Value), stored =>
                {
                    _count++;
                    _state += stored.Value;
                });
                return true;
            
            case Ready _:
                Sender.Tell(Done.Instance);
                return true;
            
            case Finish _:
                Sender.Tell(_count);
                return true;
            
            default:
                return false;
        }
    }

    public override string PersistenceId { get; }
}