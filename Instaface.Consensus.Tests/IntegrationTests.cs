namespace Instaface.Consensus.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Consensus;
    using FluentAssertions;
    using Xunit;

    public class TestCluster : IConsensusTransport
    {
        private readonly CancellationTokenSource _quit = new CancellationTokenSource();
        private List<Task> _executions;

        public ConcurrentDictionary<string, ConsensusNode> Nodes = new ConcurrentDictionary<string, ConsensusNode>();

        public TestCluster(int nodes)
        {            
            for (var n = 0; n < nodes; n++)
            {
                var name = $"node{n:00}";
                Nodes[name] = new ConsensusNode(this, new TestConfig { Self = name });
            }

            Start();
        }

        private static async Task<bool?> FailToSend()
        {
            await Task.Delay(10);
            return null;
        }

        public async Task<ICollection<(string node, Task<bool?> response)>> SendHeartbeat(string from, int term, CancellationToken cancellation)
        {
            if (!Nodes.ContainsKey(from))
            {
                return Nodes.Select(n => (n.Key, FailToSend())).ToList();
            }

            return Nodes.Where(n => n.Key != from)
                     .Select(n => (n.Key, Task.Run(async () => (bool?) await n.Value.HandleHeartbeat(term, from), cancellation)))
                     .ToList();
        }

        public void Start()
        {
            _executions = Nodes.Values.Select(n => n.Execute(_quit.Token)).ToList();
        }

        public Task Stop()
        {
            _quit.Cancel();

            return Task.WhenAll(_executions);
        }
        
        public string GotConsensus()
        {
            var leaders = Nodes.Values.Select(n => n.State).Where(n => n.Mode == ConsensusModes.Leader).ToList();
            if (leaders.Count == 1)
            {
                var name = leaders[0].Leader;

                var opinions = Nodes.Values.Select(n => n.State.Leader).Distinct().ToList();
                if (opinions.Count == 1 && opinions[0] == name)
                {
                    return name;
                }
            }
            else if (leaders.Count > 1)
            {
                // Must all be leaders in different terms
                var terms = leaders.Select(l => l.Term).Distinct().ToList();
                terms.Should().HaveCount(leaders.Count);
            }

            return null;
        }

        public async Task<string> AchieveConsensus()
        {
            var leader = new List<string>();
            
            while (leader.Count < 10 || leader.Distinct().Count() > 1)
            {                
                await Task.Delay(100);

                var newLeader = GotConsensus();
                if (newLeader != null)
                {
                    leader.Add(newLeader);
                }

                if (leader.Count > 10)
                {
                    leader.RemoveAt(0);
                }
            }
            
            return leader[0];
        }        
    }

    public class TestConfig : IConsensusConfig
    {
        public string Self { get; set; }
        public int ElectionPeriodMin => 50;
        public int ElectionPeriodMax => 100;
        public int HeartbeatPeriod => 20;
       
        public Action<string> Log => msg =>
        {
            Debug.WriteLine($"{Self}:{msg}");
        };

        public void PublishLeader(int term) { }
        public void PublishFollower(string leader, int term) { }
        public void PublishReachable(string node, int term, bool reachable) { }
        public void RaiseEvent(string type, object info = null) { }
    }

    public class IntegrationTests
    {
        [Fact]
        public async Task AchievesConsensus()
        {
            for (var nodeCount = 1; nodeCount < 10; nodeCount ++)
            {
                for (var reps = 0; reps < 10; reps++)
                {
                    var cluster = new TestCluster(nodeCount);
                    
                    await cluster.AchieveConsensus();

                    await cluster.Stop();
                }
            }
        }

        [Fact]
        public async Task FailsOver()
        {
            for (var nodeCount = 2; nodeCount < 10; nodeCount++)
            {
                for (var reps = 0; reps < 10; reps++)
                {
                    Debug.WriteLine("-----------------------------------------------------");

                    var cluster = new TestCluster(nodeCount);

                    var leaderName = await cluster.AchieveConsensus();

                    Debug.WriteLine($"<------------------ Disconnecting {leaderName}");

                    cluster.Nodes.TryRemove(leaderName, out var leader);
                    
                    var newLeaderName = await cluster.AchieveConsensus();

                    newLeaderName.Should().NotBe(leaderName);

                    await cluster.Stop();
                }
            }
        }
    }
}
