namespace Instaface.Consensus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConsensusTransport
    {
        Task<ICollection<Task<bool?>>> SendHeartbeat(string from, int term, CancellationToken cancellation);
    }
}