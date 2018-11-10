namespace Instaface
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Consensus;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Monitoring;
    using Newtonsoft.Json.Linq;

    public interface IClusterConfig : IConsensusConfig
    {
        Task<IReadOnlyList<string>> GetOtherNodes();

        string CurrentLeader { get; }

        IReadOnlyList<string> CurrentFollowers { get; }

        IReadOnlyList<string> CurrentlyUnreachable { get; }

        IReadOnlyList<string> ShuffledFollowers { get; }

        Task WaitForFollowers();

        void SetUnplugged(string node, bool unplugged);

        bool IsUnplugged(string node);

        IReadOnlyList<string> Unplugged { get; }
    }

    public class ClusterConfig : IClusterConfig
    {
        private readonly IRedis _redis;
        private readonly IMemoryCache _cache;
        private readonly IMonitoringEvents _monitoring;
        private readonly ILogger _logger;
        private readonly string _selfId;

        private readonly ConcurrentDictionary<string, bool> _reachable = new ConcurrentDictionary<string, bool>();

        private const string ConsensusNodesKey = "consensus:nodes";
        private const string ConsensusLeaderKey = "consensus:leader";
        private const string ConsensusFollowerKeyPrefix = "consensus:follower:";

        private readonly Random _random = new Random();

        private readonly HashSet<string> _unplugged = new HashSet<string>();

        public ClusterConfig(IConfiguration configuration, IRedis redis, IMemoryCache cache, ILogger<ClusterConfig> logger, IMonitoringEvents monitoring)
        {
            _redis = redis;
            _cache = cache;
            _logger = logger;
            _monitoring = monitoring;

            var consensus = configuration.GetSection("Consensus");
            _selfId = consensus.GetValue<string>("Self");
            ElectionPeriodMin = consensus.GetValue("ElectionPeriodMin", 2400);
            ElectionPeriodMax = consensus.GetValue("ElectionPeriodMax", 3600);
            HeartbeatPeriod = consensus.GetValue("HeartbeatPeriod", 1000);
            Log = msg => _logger.LogInformation(msg);

            Poll(CancellationToken.None).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    _logger.LogError(t.Exception, "PublishSelf terminated with an error");
                }
            });
        }

        public string Self { get; private set; }
        public int ElectionPeriodMin { get; }
        public int ElectionPeriodMax { get; }
        public int HeartbeatPeriod { get; }
        public Action<string> Log { get; }

        public string CurrentLeader { get; private set; }

        public IReadOnlyList<string> CurrentFollowers { get; private set; } = new List<string>();

        public IReadOnlyList<string> CurrentlyUnreachable =>
            _reachable.Where(r => !r.Value).Select(r => r.Key).ToList();

        public IReadOnlyList<string> ShuffledFollowers
        {
            get
            {
                var followers = CurrentFollowers;
                if (followers.Count == 0)
                {
                    if (CurrentLeader == null)
                    {
                        throw new InvalidOperationException("No known followers or leaders");
                    }

                    _logger.LogWarning("Using leader due to scarcity of followers");
                    return new[] {CurrentLeader};
                }

                var shuffle = followers.ToList();
                lock (_random)
                {
                    for (var n = 0; n < shuffle.Count; n++)
                    {
                        var swapWith = _random.Next(n, shuffle.Count);
                        if (swapWith != n)
                        {
                            var swapping = shuffle[n];
                            shuffle[n] = shuffle[swapWith];
                            shuffle[swapWith] = swapping;
                        }
                    }

                    return shuffle;
                }
            }
        }

        public async Task WaitForFollowers()
        {
            while (CurrentFollowers.Count == 0 || string.IsNullOrWhiteSpace(CurrentLeader))
            {
                await Task.Delay(500);
            }
        }

        public void SetUnplugged(string node, bool unplugged)
        {
            lock (_unplugged)
            {
                if (unplugged)
                {
                    _unplugged.Add(node);
                }
                else
                {
                    _unplugged.Remove(node);
                }                
            }
        }

        public bool IsUnplugged(string node)
        {
            lock (_unplugged)
            {
                return _unplugged.Contains(node);
            }
        }

        public IReadOnlyList<string> Unplugged
        {
            get
            {
                lock (_unplugged)
                {
                    return _unplugged.ToList();
                }
            }            
        }

        public void RaiseEvent(string type, object info = null)
        {
            var jObj = info == null ? new JObject() : JObject.FromObject(info);
            jObj["type"] = type;
            jObj["source"] = Self;

            _monitoring.Event(jObj);
        }

        public void PublishLeader(int term)
        {
            Console.WriteLine($"Became leader in term {term}");
            CurrentLeader = Self;

            _reachable.Clear();

            RaiseEvent("leader", new {term});
        }

        public void PublishFollower(string leader, int term)
        {
            Console.WriteLine($"Following {leader}");
            CurrentLeader = leader;

            RaiseEvent("follower", new {leader, term});
        }

        public void PublishReachable(string about, int term, bool reachable)
        {
            if (_reachable.TryAdd(about, reachable) || 
                _reachable.TryUpdate(about, reachable, !reachable))
            {
                RaiseEvent("reachable", new {about, term, reachable});
            }
        }

        private static (string Value, DateTime Expires) ParseTtlString(string timedString, char separator = '@')
        {
            var parts = timedString.Split(separator);
            return parts.Length < 2 || !DateTime.TryParse(parts[1], out var dt)
                ? (null, default(DateTime))
                : (parts[0], dt);
        }

        private static string ParseTtlStringNonExpired(string timedString, char separator = '@')
        {
            var (value, expires) = ParseTtlString(timedString, separator);
            return expires < DateTime.UtcNow ? null : value;
        }

        private static string BuildTtlString(string value, DateTime expires, char separator = '@')
        {
            return $"{value}{separator}{expires:o}";
        }

        private static string BuildTtlString(string value, TimeSpan ttl, char separator = '@')
        {
            return BuildTtlString(value, DateTime.UtcNow.Add(ttl), separator);
        }

        private async Task<IReadOnlyCollection<string>> GetAllNodes()
        {
            var nodes = _cache.Get<IReadOnlyCollection<string>>(ConsensusNodesKey);
            if (nodes == null)
            {
                nodes = (await _redis.Database.SetMembersAsync(ConsensusNodesKey))
                                     .Select(n => ParseTtlString(n.ToString()).Value)
                                     .Where(n => !string.IsNullOrWhiteSpace(n))
                                     .Distinct()
                                     .ToList();

                _cache.Set(ConsensusNodesKey, nodes, TimeSpan.FromSeconds(20));
            }
            
            return nodes;
        }

        public async Task<IReadOnlyList<string>> GetOtherNodes()
        {            
            return  (await GetAllNodes()).Where(n => n != Self).ToList();
        }
        
        private async Task Poll(CancellationToken quit)
        {
            while (!quit.IsCancellationRequested)
            {
                try
                {
                    if (_selfId != null)
                    {
                        if (Self == null)
                        {
                            Self = await _redis.Database.StringGetAsync($"consensus:ip:{_selfId}");
                        }
                        else
                        {
                            var oldTimes = (await _redis.Database.SetMembersAsync(ConsensusNodesKey))
                                           .Select(t => new { parsed = ParseTtlString(t), str = t })
                                           .Where(t => t.parsed.Value == Self)
                                           .Select(t => t.str)
                                           .ToList();

                            var tran = _redis.Database.CreateTransaction();

                            var parts = new List<Task>();
                            parts.AddRange(oldTimes.Select(o => tran.SetRemoveAsync(ConsensusNodesKey, o)));
                            parts.Add(tran.SetAddAsync(ConsensusNodesKey, BuildTtlString(Self, TimeSpan.FromMinutes(1))));

                            if (CurrentLeader == Self)
                            {
                                parts.Add(tran.KeyDeleteAsync(ConsensusFollowerKeyPrefix + Self));
                                parts.Add(tran.StringSetAsync(ConsensusLeaderKey, Self));
                            }
                            else
                            {
                                parts.Add(tran.StringSetAsync(ConsensusFollowerKeyPrefix + Self, CurrentLeader));
                            }

                            parts.Add(tran.ExecuteAsync());

                            await Task.WhenAll(parts);
                        }
                    }
                    else
                    {
                        CurrentLeader = await _redis.Database.StringGetAsync(ConsensusLeaderKey);
                    }
                    
                    CurrentFollowers = (await GetAllNodes()).Where(n => n != CurrentLeader).ToList();
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "Unable to communicate with Redis");
                }

                int delay;
                lock (_random)
                {
                    delay = _random.Next(5000, 10000);
                }

                await Task.Delay(delay, quit);
            }
        }
    }
}