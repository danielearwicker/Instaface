﻿namespace GraphServer.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Instaface;
    using Instaface.Consensus;
    using Refit;

    public class ConsensusTransport : IConsensusTransport
    {
        private readonly IClusterConfig _config;

        public ConsensusTransport(IClusterConfig config)
        {
            _config = config;
        }

        public async Task<ICollection<(string node, Task<bool?> response)>> SendHeartbeat(string from, int term, CancellationToken cancellation)
        {            
            return (await _config.GetOtherNodes())
                   .Select(n => (n, SendHeartbeat(from, n, term, cancellation)))
                   .ToList();
        }

        private async Task<bool?> SendHeartbeat(string from, string to, int term, CancellationToken cancellation)
        {
            if (_config.IsUnplugged(from) || _config.IsUnplugged(to))
            {
                return null;
            }

            var api = RestService.For<IConsensusApi>($"{to}/api/consensus");

            try
            {
                return (await api.PostHeartbeat(term, Convert.ToBase64String(Encoding.UTF8.GetBytes(from)), cancellation)).Confirmed;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}