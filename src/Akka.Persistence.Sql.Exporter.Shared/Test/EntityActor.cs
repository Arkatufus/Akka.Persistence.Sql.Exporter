// -----------------------------------------------------------------------
//  <copyright file="PersistenceActor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Akka.Persistence.Journal;

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public sealed class EntityActor : ReceivePersistentActor
{
    public static Props Props(string id) => Actor.Props.Create(() => new EntityActor(id));

    private readonly ILoggingAdapter _log;
    private int _total;
    private int _persisted;
    private StateSnapshot _lastSnapshot = StateSnapshot.Empty;
    private StateSnapshot _savingSnapshot = StateSnapshot.Empty;

    private bool _clearing;
    private IActorRef? _sender;

    public EntityActor(string persistenceId)
    {
        _log = Context.GetLogger();
        
        PersistenceId = persistenceId;
        
        Command<int>(msg => Persist(msg, i =>
        {
            _total += i;
            _persisted++;
        }));

        Command<string>(msg => Persist(msg, str =>
        {
            _total += int.Parse(str);
            _persisted++;
        }));

        Command<ShardedMessage>(msg =>
        {
            var obj = msg.ToTagged(msg.Message);
            switch (obj)
            {
                case Tagged tagged:
                    Persist(tagged, sm =>
                    {
                        _total += ((ShardedMessage)sm.Payload).Message;
                        _persisted++;
                    });
                    break;
                default:
                    Persist(msg, sm =>
                    {
                        _total += sm.Message;
                        _persisted++;
                    });
                    break;
            }
        });

        Command<CustomShardedMessage>(msg =>
        {
            var obj = msg.ToTagged(msg.Message);
            switch (obj)
            {
                case Tagged tagged:
                    Persist(tagged, sm =>
                    {
                        _total += ((CustomShardedMessage)sm.Payload).Message;
                        _persisted++;
                    });
                    break;
                default:
                    Persist(msg, sm =>
                    {
                        _total += sm.Message;
                        _persisted++;
                    });
                    break;
            }
        });

        Command<Finish>(_ =>
        {
            Sender.Tell((PersistenceId, _lastSnapshot, _total, _persisted));
        });

        Command<TakeSnapshotAndClear>(_ =>
        {
            _sender = Sender;
            _clearing = true;
            _savingSnapshot = new StateSnapshot(_total, _persisted);
            SaveSnapshot(_savingSnapshot);
        });
        
        Command<TakeSnapshot>(_ =>
        {
            _sender = Sender;
            _savingSnapshot = new StateSnapshot(_total, _persisted);
            SaveSnapshot(_savingSnapshot);
        });
        
        Command<SaveSnapshotSuccess>(msg =>
        {
            _lastSnapshot = _savingSnapshot;
            _savingSnapshot = StateSnapshot.Empty;
            
            if(!_clearing)
            {
                _sender.Tell((PersistenceId, _lastSnapshot));
                return;
            }

            _clearing = false;
            DeleteMessages(msg.Metadata.SequenceNr);
        });
        
        Command<SaveSnapshotFailure>(fail =>
        {
            _log.Error(fail.Cause, "SaveSnapshot failed!");
            _savingSnapshot = StateSnapshot.Empty;
            _sender.Tell((PersistenceId, _lastSnapshot));
        });
        
        Command<DeleteMessagesSuccess>(_ =>
        {
            _sender.Tell((PersistenceId, _lastSnapshot));
        });
        
        Command<DeleteMessagesFailure>(fail =>
        {
            _log.Error(fail.Cause, "DeleteMessages failed!");
            _sender.Tell((PersistenceId, _lastSnapshot));
        });

        Command<RecoveryCompleted>(_ =>
        {
            _log.Info($"{persistenceId}: Recovery completed. State: [Total:{_total}, Persisted:{_persisted}.]");
        });
        
        Recover<SnapshotOffer>(offer =>
        {
            _lastSnapshot = (StateSnapshot) offer.Snapshot;
            _total = _lastSnapshot.Total;
            _persisted = _lastSnapshot.Persisted;
            _log.Info($"{persistenceId}: Snapshot loaded. State: [Total:{_total}, Persisted:{_persisted}.] " +
                      $"Metadata: [SequenceNr:{offer.Metadata.SequenceNr}, Timestamp:{offer.Metadata.Timestamp}]");
        });
        
        Recover<int>(msg =>
        {
            _total += msg;
            _persisted++;
        });
        
        Recover<string>(msg =>
        {
            _total += int.Parse(msg);
            _persisted++;
        });
        
        Recover<ShardedMessage>(msg =>
        {
            _total += msg.Message;
            _persisted++;
        });
        
        Recover<CustomShardedMessage>(msg =>
        {
            _total += msg.Message;
            _persisted++;
        });
    }

    public override string PersistenceId { get; }

    protected override void PreStart()
    {
        _log.Debug($"EntityActor({PersistenceId}) started");
        base.PreStart();
    }
}