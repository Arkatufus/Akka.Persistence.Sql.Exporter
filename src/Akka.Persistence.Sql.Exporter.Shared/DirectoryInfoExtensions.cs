// -----------------------------------------------------------------------
//  <copyright file="DirectoryInfoExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

namespace Akka.Persistence.Sql.Exporter.Shared;

public static class DirectoryInfoExtensions
{
    public static void DeleteRecursive(this DirectoryInfo baseDir)
    {
        if(!baseDir.Exists)
            return;

        foreach (var dir in baseDir.EnumerateDirectories())
            dir.DeleteRecursive();

        foreach (var file in baseDir.GetFiles())
            file.Delete();
            
        baseDir.Delete(true);
    }

    public static void Clear(this DirectoryInfo dir)
    {
        dir.DeleteRecursive();
        dir.Create();
    }
}