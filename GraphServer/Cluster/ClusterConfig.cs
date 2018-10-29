namespace GraphServer.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Instaface;
    using Instaface.Consensus;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    public interface IClusterConfig : IConsensusConfig
    {
        Task<IReadOnlyCollection<string>> GetOtherNodes();
    }

    public class ClusterConfig : IClusterConfig
    {
        private readonly IRedis _redis;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;

        private string _publishedLeader;

        private const string ConsensusNodesKey = "consensus:nodes";
        private const string ConsensusLeaderKey = "consensus:leader";
        private const string ConsensusFollowerKeyPrefix = "consensus:follower:";

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

            PublishSelf(CancellationToken.None).ContinueWith(t =>
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

        public void PublishLeader()
        {
            _publishedLeader = Self;
        }

        public void PublishFollower(string leader)
        {
            _publishedLeader = leader;
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
    
        public Task<IReadOnlyCollection<string>> GetOtherNodes()
        {            
            return _cache.GetOrCreateAsync<IReadOnlyCollection<string>>(ConsensusNodesKey, async e => 
                (await _redis.Database.SetMembersAsync(ConsensusNodesKey))
                             .Select(n => ParseTtlStringNonExpired(n.ToString()))
                             .Where(n => !string.IsNullOrWhiteSpace(n) && n != Self)
                             .Distinct()
                             .ToList());
        }

        private async Task PublishSelf(CancellationToken quit)
        {
            while (!quit.IsCancellationRequested)
            {
                try
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

                    if (_publishedLeader == Self)
                    {
                        parts.Add(tran.KeyDeleteAsync(ConsensusFollowerKeyPrefix + Self));
                        parts.Add(tran.StringSetAsync(ConsensusLeaderKey, Self));
                    }
                    else
                    {
                        parts.Add(tran.StringSetAsync(ConsensusFollowerKeyPrefix + Self, _publishedLeader));
                    }

                    parts.Add(tran.ExecuteAsync());

                    await Task.WhenAll(parts);
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "PublishSelf unable to write to Redis");
                }

                await Task.Delay(30, quit);
            }
        }
    }
}