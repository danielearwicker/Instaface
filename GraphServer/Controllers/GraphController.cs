using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Instaface;
using Instaface.Caching;
using Newtonsoft.Json.Linq;

namespace GraphServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphController : ControllerBase, IGraphCache, IGraphLeader
    {                
        private readonly IGraphCache _graph;
        private readonly IClusterConfig _cluster;

        public GraphController(IGraphCache graph, IClusterConfig cluster)
        {
            _graph = graph;
            _cluster = cluster;
        }
        
        [HttpPost("entities")]
        public Task<IReadOnlyCollection<Entity>> GetEntities([FromBody] IEnumerable<int> ids)
        {
            return _graph.GetEntities(ids);
        }

        [HttpPost("entities/{type}")]
        public Task<IReadOnlyCollection<Association>> GetAssociations([FromBody] IEnumerable<int> ids, string type)
        {
            return _graph.GetAssociations(ids, type);
        }

        [HttpPost("query")]
        public Task<JObject> Query(QueryRequest request)
        {
            _cluster.RaiseEvent("query", new { ids = request.Entities });
            return _graph.Query(request);
        }

        private IGraphDataWrite GetWriteTarget()
        {
            return _cluster.Self == _cluster.CurrentLeader ? _graph : _cluster.Write();
        }

        [HttpPost("entities/{type}/create")]
        public Task<int> CreateEntity(string type, [FromBody] JObject attributes)
        {
            return GetWriteTarget().CreateEntity(type, attributes);
        }

        [HttpPost("associations/{type}/from/{from}/to/{to}")]
        public Task CreateAssociation(int from, int to, string type, [FromBody] JObject attributes)
        {
            return GetWriteTarget().CreateAssociation(from, to, type, attributes);
        }

        [HttpPost("random/{type}/{count}")]
        public Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            return _graph.GetRandomEntities(type, count);
        }

        [HttpGet("unreachable")]
        public Task<IReadOnlyList<string>> GetUnreachableNodes()
        {
            return Task.FromResult(_cluster.CurrentlyUnreachable);
        }

        [HttpGet("unplugged")]
        public Task<IReadOnlyList<string>> GetUnpluggedNodes()
        {
            return Task.FromResult(_cluster.Unplugged);
        }

        [HttpPut("unplugged")]
        public Task<NodeUnplugState> PutUnpluggedNode([FromBody] NodeUnplugState state)
        {
            _cluster.SetUnplugged(state.Node, state.Unplugged);
            return Task.FromResult(state);
        }
    }
}
