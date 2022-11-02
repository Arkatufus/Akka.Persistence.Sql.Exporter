// -----------------------------------------------------------------------
//  <copyright file="CustomSerializer.cs" company="Akka.NET Project">
//      Copyright (C) 2009-2022 Lightbend Inc. <http://www.lightbend.com>
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Serialization;

namespace Akka.Persistence.Sql.Compat.Common
{
    public sealed class CustomSerializer: SerializerWithStringManifest
    {
        public const string CustomShardedMessageManifest = "CSM";
    
        public CustomSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override int Identifier => 999;

        public override byte[] ToBinary(object obj)
        {
            if (obj is not CustomShardedMessage msg)
                throw new Exception($"Can only process {nameof(CustomShardedMessage)}");

            return BitConverter.GetBytes(msg.Message);
        }

        public override object FromBinary(byte[] bytes, string manifest)
        {
            if(manifest != CustomShardedMessageManifest)
                throw new Exception($"Can only process {nameof(CustomShardedMessage)}");

            return new CustomShardedMessage(BitConverter.ToInt32(bytes, 0));
        }

        public override string Manifest(object obj)
        {
            if (obj is not CustomShardedMessage)
                throw new Exception($"Can only process {nameof(CustomShardedMessage)}");
            return CustomShardedMessageManifest;
        }
    }    
}
