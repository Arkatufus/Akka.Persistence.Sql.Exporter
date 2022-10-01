// -----------------------------------------------------------------------
//  <copyright file="Messages.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared;

public sealed class Store
{
    public readonly int Value;

    public Store(int value)
    {
        Value = value;
    }
}

public sealed class Stored
{
    public readonly int Value;

    public Stored(int value)
    {
        Value = value;
    }
}

public sealed class Finish
{
    public static readonly Finish Instance = new();
    private Finish() { }
}

public sealed class Ready
{
    public static readonly Ready Instance = new();
    private Ready() { }
}
