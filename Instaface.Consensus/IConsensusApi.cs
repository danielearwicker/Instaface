namespace Instaface.Consensus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Refit;

    public interface IConsensusApi
    {
        [Post("/heartbeat/{term}/from/{from}")]
        Task<HeartbeatResponse> PostHeartbeat(int term, string from, CancellationToken cancellation);
    }
}
