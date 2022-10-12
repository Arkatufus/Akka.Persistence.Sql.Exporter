// -----------------------------------------------------------------------
//  <copyright file="EventAdapter.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Persistence.Journal;

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public sealed class EventAdapter: IWriteEventAdapter
{
    public string Manifest(object evt) => string.Empty;

    public object ToJournal(object evt)
    {
        var value = evt switch
        {
            int i => i,
            string str => int.Parse(str),
            _ => throw new Exception($"Unknown type: {evt.GetType()}")
        };
        
        return evt.ToTagged(value);
    }
}