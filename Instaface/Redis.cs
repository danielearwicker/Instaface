using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Instaface
{
    using System;
    using System.Linq;
    using System.Net;

    public interface IRedis
    {
        IDatabase Database { get; }        
    }

    public class Redis : IRedis
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly EndPoint _server;

        public Redis(IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetSection("Redis").GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                throw new InvalidOperationException("Missing configuration Redis:ConnectionString");
            }

            _redis = ConnectionMultiplexer.Connect(redisConnectionString);

            _server = _redis.GetEndPoints().Single();
        }
        
        public IDatabase Database => _redis.GetDatabase();        
    }    
}