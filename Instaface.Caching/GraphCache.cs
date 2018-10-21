using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Instaface.Caching
{
    public interface IGraphCache : IGraphQuery, IGraphData { }

    public class GraphCache : IGraphCache
    {
        private readonly IGraphData _source;
        private readonly ILogger<GraphCache> _logger;
        private readonly object _locker = new object();

        private readonly EntityCache _entities;

        private readonly Dictionary<string, AssociationCache> _associations =
                     new Dictionary<string, AssociationCache>();

        private AssociationCache GetAssociationCache(string type)
        {
            lock (_locker)
            {
                return _associations.Get(type, () => new AssociationCache(_source, type));
            }
        }

        public GraphCache(IGraphData source, ILogger<GraphCache> logger)
        {
            _source = source;
            _logger = logger;
            _entities = new EntityCache(_source);
        }
        
        public Task<IReadOnlyCollection<Entity>> GetEntities(IEnumerable<int> ids)
        {
            return _entities.Get(ids);
        }

        public async Task<IReadOnlyCollection<Association>> GetAssociations(IEnumerable<int> ids, string type)
        {
            return (await GetAssociationCache(type).Get(ids)).SelectMany(a => a).ToList();
        }

        public Task<int> CreateEntity(string type, JObject attributes)
        {
            return _source.CreateEntity(type, attributes);
        }

        public async Task CreateAssociation(int from, int to, string type, JObject attributes = null)
        {
            await _source.CreateAssociation(from, to, type, attributes);
            GetAssociationCache(type).Delete(from);
        }

        public Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            return _source.GetRandomEntities(type, count);
        }

        public async Task<JArray> Query(QueryRequest request)
        {
            var engine = new QueryEngine(this);
                
            var result = await engine.Query(request);

            _logger.LogInformation("Query required {Calls} calls into the cache", engine.Calls);
            return result;
        }
    }
}