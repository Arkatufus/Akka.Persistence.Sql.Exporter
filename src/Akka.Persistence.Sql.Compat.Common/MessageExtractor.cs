// -----------------------------------------------------------------------
//  <copyright file="MessageExtractor.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

using Akka.Cluster.Sharding;

namespace Akka.Persistence.Sql.Compat.Common
{
    public sealed class MessageExtractor : HashCodeMessageExtractor
    {
        /// <summary>
        /// We only ever run three nodes, so ~10 shards per node
        /// </summary>
        public MessageExtractor() : base(30)
        {
        }

        public override string? EntityId(object message)
            => message switch
            {
                int i => i.ToEntityId(),
                string str => int.Parse(str).ToEntityId(),
                IHasEntityId msg => msg.EntityId,
                ShardingEnvelope msg => msg.EntityId,
                _ => null
            };
    }
    
}
