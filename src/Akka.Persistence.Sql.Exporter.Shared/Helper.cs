// -----------------------------------------------------------------------
//  <copyright file="Exporter.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared;

public static class Env
{
    public static string OutputPath 
    {
        get
        {
            var path = Environment.GetEnvironmentVariable("OUTPUT");
            return string.IsNullOrEmpty(path) ? Path.GetFullPath("./bin/output") : path;
        }
    }
}