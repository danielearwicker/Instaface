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
        
        private async Task<T> Get<T>(T db)
        {
            await _cluster.WaitForFollowers();
            return _cluster.CurrentLeader == _cluster.Self ? db : GraphClient.Create<T>(_cluster.CurrentLeader);
        }
        
        public async Task<IReadOnlyCollection<Entity>> GetEntities(IEnumerable<int> id)
        {
            return await (await Get<IGraphDataRead>(_db)).GetEntities(id);
        }

        public async Task<IReadOnlyCollection<Association>> GetAssociations(IEnumerable<int> id, string type)
        {
            return await(await Get<IGraphDataRead>(_db)).GetAssociations(id, type);
        }

        public async Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            return await(await Get<IGraphDataRead>(_db)).GetRandomEntities(type, count);
        }

        public async Task<int> CreateEntity(string type, JObject attributes)
        {
            return await(await Get<IGraphDataWrite>(_db)).CreateEntity(type, attributes);
        }

        public async Task CreateAssociation(int from, int to, string type, JObject attributes)
        {
            await(await Get<IGraphDataWrite>(_db)).CreateAssociation(from, to, type, attributes);
        }
    }
}