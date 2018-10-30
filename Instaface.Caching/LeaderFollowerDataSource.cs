namespace Instaface.Caching
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public class LeaderFollowerDataSource : IGraphData
    {
        private readonly IClusterConfig _cluster;

        private readonly IGraphData _db;

        public LeaderFollowerDataSource(IClusterConfig cluster, IGraphData db)
        {
            _cluster = cluster;
            _db = db;
        }
        
        private IGraphData GetSource()
        {
            return _cluster.CurrentLeader == _cluster.Self ? _db : _cluster.Leader();
        }

        public Task<IReadOnlyCollection<Entity>> GetEntities(IEnumerable<int> id)
        {
            return GetSource().GetEntities(id);
        }

        public Task<IReadOnlyCollection<Association>> GetAssociations(IEnumerable<int> id, string type)
        {
            return GetSource().GetAssociations(id, type);
        }

        public Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            return GetSource().GetRandomEntities(type, count);
        }

        public Task<int> CreateEntity(string type, JObject attributes)
        {
            return GetSource().CreateEntity(type, attributes);
        }

        public Task CreateAssociation(int from, int to, string type, JObject attributes)
        {
            return GetSource().CreateAssociation(from, to, type, attributes);
        }
    }
}