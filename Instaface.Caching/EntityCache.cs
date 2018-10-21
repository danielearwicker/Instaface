using System.Collections.Generic;
using System.Threading.Tasks;

namespace Instaface.Caching
{
    public class EntityCache : AsyncCache<Entity>
    {
        private readonly IGraphData _source;

        public EntityCache(IGraphData source)
        {
            _source = source;
        }

        protected override Task<IReadOnlyCollection<Entity>> GetItems(IEnumerable<int> ids)
        {
            return _source.GetEntities(ids);
        }
        
        protected override int GetId(Entity item)
        {
            return item.Id;
        }
    }
}