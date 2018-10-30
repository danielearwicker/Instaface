namespace Instaface
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Consensus;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    
    public interface IClusterConfig : IConsensusConfig
    {
        Task<IReadOnlyList<string>> GetOtherNodes();

        string CurrentLeader { get; }

        IReadOnlyList<string> CurrentFollowers { get; }

        string RandomFollower { get; }
    }

    public class ClusterConfig : IClusterConfig
    {
        private readonly IRedis _redis;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;
        
        private const string ConsensusNodesKey = "consensus:nodes";
        private const string ConsensusLeaderKey = "consensus:leader";
        private const string ConsensusFollowerKeyPrefix = "consensus:follower:";

        private readonly Random _random = new Random();

        public ClusterConfig(IConfiguration configuration, IRedis redis, IMemoryCache cache, ILogger<ClusterConfig> logger)
        {
            _redis = redis;
            _cache = cache;
            _logger = logger;

            var consensus = configuration.GetSection("Consensus");
            Self = consensus.GetValue<string>("Self");
            ElectionPeriodMin = consensus.GetValue("ElectionPeriodMin", 1200);
            ElectionPeriodMax = consensus.GetValue("ElectionPeriodMax", 1700);
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

        public string Self { get; }
        public int ElectionPeriodMin { get; }
        public int ElectionPeriodMax { get; }
        public int HeartbeatPeriod { get; }
        public Action<string> Log { get; }

        public string CurrentLeader { get; private set; }

        public IReadOnlyList<string> CurrentFollowers { get; private set; } = new List<string>();

        public string RandomFollower
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
                    return CurrentLeader;
                }

                lock (_random)
                {
                    return followers[_random.Next(0, followers.Count)];
                }
            }
        }

        public void PublishLeader()
        {
            CurrentLeader = Self;
        }

        public void PublishFollower(string leader)
        {
            CurrentLeader = leader;
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

        private Task<IReadOnlyCollection<string>> GetAllNodes()
        {
            return _cache.GetOrCreateAsync<IReadOnlyCollection<string>>(ConsensusNodesKey, async e =>
                (await _redis.Database.SetMembersAsync(ConsensusNodesKey))
                .Select(n => ParseTtlStringNonExpired(n.ToString()))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList());
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
                    if (Self != null)
                    {
                        var oldTimes = (await _redis.Database.SetMembersAsync(ConsensusNodesKey))
                                       .Select(t => new {parsed = ParseTtlString(t), str = t})
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