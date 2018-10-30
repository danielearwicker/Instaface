using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using Newtonsoft.Json.Linq;

namespace Instaface
{
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

    public static class GraphClient
    {
        public static T Create<T>(string server) => RestService.For<T>($"{server}/api/graph");
        
        public static IGraphDataRead Read(this IClusterConfig config) => Create<IGraphDataRead>(config.RandomFollower);

        public static IGraphDataWrite Write(this IClusterConfig config) => config.Leader();

        public static IGraphData Leader(this IClusterConfig config) => Create<IGraphData>(config.CurrentLeader);

        public static IGraphQuery Query(this IClusterConfig config) => Create<IGraphQuery>(config.RandomFollower);

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
