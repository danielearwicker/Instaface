namespace Instaface.Consensus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConsensusTransport
    {
        Task<ICollection<(string node, Task<bool?> response)>> SendHeartbeat(string from, int term, CancellationToken cancellation);
    }
}