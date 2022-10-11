// -----------------------------------------------------------------------
//  <copyright file="EventAdapter.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Persistence.Journal;

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public sealed class EventAdapter: IWriteEventAdapter
{
    private readonly string[] _tags = DataGenerator.Tags;
    public string Manifest(object evt) => string.Empty;

    public object ToJournal(object evt)
    {
        if (evt is not int && evt is not string)
            return evt;
        
        var value = evt switch
        {
            int i => i,
            string str => int.Parse(str),
            _ => throw new Exception($"Unknown type: {evt.GetType()}")
        };
        
        return (value % 4) switch
        {
            0 => evt,
            1 => new Tagged(evt, new[]{ _tags[0] }),
            2 => new Tagged(evt, new[]{ _tags[0], _tags[1] }),
            _ => new Tagged(evt, new[]{ _tags[0], _tags[1], _tags[2] })
        };
    }
}