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
    public class GraphController : ControllerBase, IGraphCache
    {                
        private readonly IGraphCache _graph;

        public GraphController(IGraphCache graph)
        {
            _graph = graph;
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
            return _graph.Query(request);
        }

        [HttpPost("entities/{type}/create")]
        public Task<int> CreateEntity(string type, [FromBody] JObject attributes)
        {
            return _graph.CreateEntity(type, attributes);
        }

        [HttpPost("associations/{type}/from/{from}/to/{to}")]
        public Task CreateAssociation(int from, int to, string type, [FromBody] JObject attributes)
        {
            return _graph.CreateAssociation(from, to, type, attributes);
        }

        [HttpPost("random/{type}/{count}")]
        public Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            return _graph.GetRandomEntities(type, count);
        }
    }
}
