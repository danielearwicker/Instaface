using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Instaface
{
    using System.Linq;
    using System.Net;

    public interface IRedis
    {
        IDatabase Database { get; }

        IServer Server { get; }
    }

    public class Redis : IRedis
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly EndPoint _server;

        public Redis(IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetSection("Redis").GetValue<string>("ConnectionString");

            _redis = ConnectionMultiplexer.Connect(redisConnectionString);

            _server = _redis.GetEndPoints().Single();
        }
        
        public IDatabase Database => _redis.GetDatabase();

        public IServer Server => _redis.GetServer(_server);
    }
    
}