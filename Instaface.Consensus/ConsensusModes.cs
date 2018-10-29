namespace Instaface.Consensus
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate Task ConsensusMode(IConsensusInternals node);

    public static class ConsensusModes
    {
        public static readonly ConsensusMode Follower = async node =>
        {
            var timeLimit = Task.Delay(node.Random.Next(node.Config.ElectionPeriodMin, node.Config.ElectionPeriodMax));
            
            while (!timeLimit.IsCompleted)
            {
                await Task.WhenAny(timeLimit, node.HeartbeatReceived);

                if (node.RespondToHeartbeat())
                {
                    return;
                }
            }

            node.BeginCandidacy();
        };

        public static readonly ConsensusMode Candidate = async node =>
        {
            var cancellation = new CancellationTokenSource();
            
            var requests = await node.SendHeartbeat(cancellation.Token);
            if (requests == null)
            {
                // Apparently not a candidate now
                return;
            }

            var nodes = requests.Count + 1;
            var votesRequired = nodes / 2; // plus ours -> majority

            var timeLimit = Task.Delay(node.Config.ElectionPeriodMax);
            
            while (!timeLimit.IsCompleted && votesRequired > 0 && requests.Count > 0)
            {
                node.Config.Log($"requires {votesRequired} votes");

                var completed = await Task.WhenAny(requests.Concat(new[] { timeLimit, node.HeartbeatReceived }));

                if (node.RespondToHeartbeat())
                {
                    break;
                }
                
                var vote = requests.FirstOrDefault(r => r == completed);
                if (vote != null)
                {
                    requests.Remove(vote);

                    if (await vote == true)
                    {
                        votesRequired--;
                    }
                }
            }

            cancellation.Cancel();

            if (votesRequired > 0)
            {
                node.EndCandidacy();                
            }
            else
            {
                node.BecomeLeader();
            }
        };

        public static readonly ConsensusMode Leader = async node =>
        {
            var cancellation = new CancellationTokenSource();

            var tasks = await node.SendHeartbeat(cancellation.Token);

            var timeLimit = Task.Delay(node.Config.HeartbeatPeriod);
            
            while (!timeLimit.IsCompleted)
            {
                await Task.WhenAny(timeLimit, node.HeartbeatReceived);

                if (node.RespondToHeartbeat())
                {
                    break;
                }
            }

            cancellation.Cancel();

            var grouped = tasks.ToLookup(t => !t.IsCompleted || t.IsFaulted || t.IsCanceled ? default(bool?) : t.Result);
            var followers = grouped[true];
            var objectors = grouped[false];
            var unknown = grouped[null];
            node.Config.Log($"followers: {followers.Count()}, objectors: {objectors.Count()}, unknown: {unknown.Count()}");
        };
    }
}
