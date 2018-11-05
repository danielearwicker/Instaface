using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using Newtonsoft.Json.Linq;

namespace Instaface
{
    using System;

    public interface IGraphDataRead
    {
        [Post("/entities")]
        Task<IReadOnlyCollection<Entity>> GetEntities([Body] IEnumerable<int> id);

        [Post("/entities/{type}")]
        Task<IReadOnlyCollection<Association>> GetAssociations([Body] IEnumerable<int> id, string type);
        
        [Post("/random/{type}/{count}")]
        Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count);
    }

    public interface IGraphDataWrite
    {
        [Post("/entities/{type}/create")]
        Task<int> CreateEntity(string type, [Body] JObject attributes);

        [Post("/associations/{type}/from/{from}/to/{to}")]
        Task CreateAssociation(int from, int to, string type, [Body] JObject attributes);        
    }

    public interface IGraphData : IGraphDataRead, IGraphDataWrite { }

    public interface IGraphQuery
    {
        [Post("/query")]
        Task<JObject> Query(QueryRequest request);
    }

    public class NodeUnplugState
    {
        public string Node { get; set; }
        public bool Unplugged { get; set; }
    }

    public interface IGraphLeader
    {
        [Get("/unreachable")]
        Task<IReadOnlyList<string>> GetUnreachableNodes();

        [Get("/unplugged")]
        Task<IReadOnlyList<string>> GetUnpluggedNodes();

        [Put("/unplugged")]
        Task<NodeUnplugState> PutUnpluggedNode([Body] NodeUnplugState state);
    }

    public class FailOverClient : IGraphDataRead, IGraphQuery
    {
        private readonly IReadOnlyList<string> _uris;

        public FailOverClient(IReadOnlyList<string> uris)
        {
            _uris = uris;
        }

        private T Retry<T>(Func<string, T> attempt)
        {
            Exception lastException = null;

            foreach (var uri in _uris)
            {
                try
                {
                    return attempt(uri);
                }
                catch (Exception x)
                {
                    lastException = x;
                }
            }

            throw lastException ?? new InvalidOperationException();
        }

        public Task<IReadOnlyCollection<Entity>> GetEntities(IEnumerable<int> id)
        {
            return Retry(uri => GraphClient.Create<IGraphDataRead>(uri).GetEntities(id));
        }

        public Task<IReadOnlyCollection<Association>> GetAssociations(IEnumerable<int> id, string type)
        {
            return Retry(uri => GraphClient.Create<IGraphDataRead>(uri).GetAssociations(id, type));
        }

        public Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            return Retry(uri => GraphClient.Create<IGraphDataRead>(uri).GetRandomEntities(type, count));
        }

        public Task<JObject> Query(QueryRequest request)
        {
            return Retry(uri => GraphClient.Create<IGraphQuery>(uri).Query(request));
        }
    }

    public static class GraphClient
    {
        public static T Create<T>(string server)
        {
            var url = $"{server}/api/graph";
            Console.WriteLine($"{typeof(T).Name} at {url}");
            return RestService.For<T>(url);
        }

        public static IGraphDataRead Read(this IClusterConfig config) => new FailOverClient(config.ShuffledFollowers);

        public static IGraphDataWrite Write(this IClusterConfig config) => Create<IGraphDataWrite>(config.CurrentLeader);

        public static IGraphLeader Leader(this IClusterConfig config) => Create<IGraphLeader>(config.CurrentLeader);

        public static IGraphQuery Query(this IClusterConfig config) => new FailOverClient(config.ShuffledFollowers);

        public static Task CreateAssociation(this IGraphDataWrite client, int from, int to, string type)
        {
            return client.CreateAssociation(from, to, type, new JObject());
        }

        public static async Task CreateAssociation(this IGraphDataWrite client, int from, int to, string typeFromTo, string typeToFrom)
        {
            await client.CreateAssociation(from, to, typeFromTo);
            await client.CreateAssociation(to, from, typeToFrom);
        }
    }
}
