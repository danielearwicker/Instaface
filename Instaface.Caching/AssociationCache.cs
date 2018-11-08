namespace Instaface.Caching
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class AssociationCache : AsyncCache<IReadOnlyList<Association>>
    {
        private readonly IGraphData _source;
        private readonly string _type;

        public AssociationCache(IGraphData source, string type)
        {
            _source = source;
            _type = type;
        }

        protected override async Task<IReadOnlyCollection<IReadOnlyList<Association>>> GetItems(IEnumerable<int> ids)
        {
            var all = await _source.GetAssociations(ids, _type);
            return all.GroupBy(a => a.From).Select(a => a.ToList()).ToList();            
        }
        
        protected override int GetId(IReadOnlyList<Association> item)
        {
            return item[0].From;
        }
    }
}