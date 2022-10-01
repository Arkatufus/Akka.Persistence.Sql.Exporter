// -----------------------------------------------------------------------
//  <copyright file="ActorSystemExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;

namespace Akka.Persistence.Sql.Exporter.Shared;

public static class ActorSystemExtensions
{
    private const int TotalEvents = 1000;
    
    public static async Task CreateTestData(this ActorSystem system)
    {
        var log = Logging.GetLogger(system, "SQLExporter");
        var actor1 = system.ActorOf(PersistenceActor.Props("one"));
        await actor1.Ask<Done>(Ready.Instance);
            
        log.Info($">>>>>>>>>>> Persisting {TotalEvents} events");
        foreach (var i in Enumerable.Range(0, TotalEvents))
        {
            actor1.Tell(new Store(i));
            if(i>0 && i%500 == 0)
                log.Info($">>>>>>>>>>> Queued: {i} events");
        }

        log.Info(">>>>>>>>>>> Waiting for all events to be persisted");
        var count = 0;
        while (count < TotalEvents)
        {
            count = (int) await actor1.Ask<long>(Finish.Instance);
            log.Info($">>>>>>>>>>> Persisted: {count} events");
        }
    }
}