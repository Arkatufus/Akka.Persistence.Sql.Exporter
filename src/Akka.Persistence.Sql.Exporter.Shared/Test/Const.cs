// -----------------------------------------------------------------------
//  <copyright file="Const.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared.Test;

public static class Const
{
    // We're only doing 100 entities
    public const int MaxEntities = 100;
    
    public static readonly string[] Tags = { "Tag1", "Tag2", "Tag3", "Tag4" };
    
}