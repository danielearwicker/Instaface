using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using Newtonsoft.Json.Linq;

namespace Instaface
{
    public interface IGraphData
    {
        [Post("/entities")]
        Task<IReadOnlyCollection<Entity>> GetEntities([Body] IEnumerable<int> id);

        [Post("/entities/{type}")]
        Task<IReadOnlyCollection<Association>> GetAssociations([Body] IEnumerable<int> id, string type);

        [Post("/entities/{type}/create")]
        Task<int> CreateEntity(string type, [Body] JObject attributes);

        [Post("/associations/{type}/from/{from}/to/{to}")]
        Task CreateAssociation(int from, int to, string type, [Body] JObject attributes);

        [Post("/random/{type}/{count}")]
        Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count);
    }

    public interface IGraphQuery
    {
        [Post("/query")]
        Task<JArray> Query(QueryRequest request);
    }

    public static class GraphClient
    {
        public static T Create<T>(string server) => RestService.For<T>($"{server}/api/graph");

        public static IGraphData Data(string server) => Create<IGraphData>(server);

        public static IGraphQuery Query(string server) => Create<IGraphQuery>(server);

        public static Task CreateAssociation(this IGraphData client, int from, int to, string type)
        {
            return client.CreateAssociation(from, to, type, new JObject());
        }

        public static async Task CreateAssociation(this IGraphData client, int from, int to, string typeFromTo, string typeToFrom)
        {
            await client.CreateAssociation(from, to, typeFromTo);
            await client.CreateAssociation(to, from, typeToFrom);
        }
    }
}
