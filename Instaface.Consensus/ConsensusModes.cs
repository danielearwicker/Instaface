namespace Instaface.Consensus
{
    using System;
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

            Console.WriteLine("Missed heartbeat!");

            node.BeginCandidacy();
        };

        public static readonly ConsensusMode Candidate = async node =>
        {
            var cancellation = new CancellationTokenSource();
            
            var requests = node.SendHeartbeat(cancellation.Token).requests;
            if (requests == null)
            {
                // Apparently not a candidate now
                return;
            }

            var responses = (await requests).Select(h => h.response).ToList();
            
            var nodes = responses.Count + 1;
            var votesRequired = nodes / 2; // plus ours -> majority

            var timeLimit = Task.Delay(node.Config.ElectionPeriodMax);
            
            while (!timeLimit.IsCompleted && votesRequired > 0 && responses.Count > 0)
            {
                node.Config.Log($"requires {votesRequired} votes");

                var completed = await Task.WhenAny(responses.Concat(new[] { timeLimit, node.HeartbeatReceived }));

                if (node.RespondToHeartbeat())
                {
                    break;
                }
                
                var vote = responses.FirstOrDefault(r => r == completed);
                if (vote != null)
                {
                    responses.Remove(vote);

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
            
            var heartbeat = node.SendHeartbeat(cancellation.Token);

            var requests = await heartbeat.requests;

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
            
            foreach (var r in requests)
            {
                var completed = r.response.IsCompleted &&
                                !r.response.IsFaulted &&
                                !r.response.IsCanceled;

                var response = completed ? await r.response : null; 
                if (response == null)
                {
                    Console.WriteLine($"Heartbeat failed to send to {r.node}");
                }

                node.Config.PublishReachable(r.node, heartbeat.term, response != null);
            }     
        };
    }
}
