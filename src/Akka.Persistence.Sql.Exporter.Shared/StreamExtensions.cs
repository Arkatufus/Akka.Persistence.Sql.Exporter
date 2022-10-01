// -----------------------------------------------------------------------
//  <copyright file="StreamExtensions.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System.Buffers;

namespace Akka.Persistence.Sql.Exporter.Shared;

public static class StreamExtensions
{
    public static async Task<byte[]> ReadAllBytes(this Stream stream, CancellationToken token)
    {
        using var ms = new MemoryStream((int) stream.Length);
        using var memoryOwner = MemoryPool<byte>.Shared.Rent(1024);
        var memory = memoryOwner.Memory;
        token.Register(() => throw new OperationCanceledException("Read operation has been cancelled", token));
        
        while (true)
        {
            var read = await stream.ReadAsync(memory, token);
            
            if (read == 0)
                break;
            
            await ms.WriteAsync(memory, token);
        }
        return ms.ToArray();
    }

    public static async Task DumpToFile(this Stream stream, string path)
    {
        if(File.Exists(path)) File.Delete(path);
        await using var outStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write);
        using var memoryOwner = MemoryPool<byte>.Shared.Rent(1024);
        var memory = memoryOwner.Memory;
        while(true)
        {
            var read = await stream.ReadAsync(memory);
            if (read == 0)
                break;
            await outStream.WriteAsync(memory[..read]);
        }
    }
}