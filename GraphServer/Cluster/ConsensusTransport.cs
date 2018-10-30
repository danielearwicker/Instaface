namespace GraphServer.Cluster
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

        public async Task<ICollection<Task<bool?>>> SendHeartbeat(string from, int term, CancellationToken cancellation)
        {            
            return (await _config.GetOtherNodes()).Select(p => SendHeartbeat(from, p, term, cancellation)).ToList();
        }

        private static async Task<bool?> SendHeartbeat(string from, string to, int term, CancellationToken cancellation)
        {
            var api = RestService.For<IConsensusApi>($"{to}/api/consensus");

            try
            {
                return (await api.PostHeartbeat(term, Convert.ToBase64String(Encoding.UTF8.GetBytes(from)), cancellation)).Confirmed;
            }
            catch (Exception)
            {
                return default (bool?);
            }
        }
    }
}