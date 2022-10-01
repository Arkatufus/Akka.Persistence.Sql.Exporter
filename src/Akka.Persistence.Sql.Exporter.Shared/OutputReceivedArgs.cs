// -----------------------------------------------------------------------
//  <copyright file="OutputReceivedArgs.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared;

public class OutputReceivedArgs : EventArgs
{
    public OutputReceivedArgs(string output)
    {
        Output = output;
    }

    public string Output { get; }
}
