namespace Instaface
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Diagnostics;

    public class QueryRequest
    {
        public IReadOnlyCollection<int> Entities { get; set; }
        public JObject Template { get; set; }
    }

    public class QueryWorkItem
    {
        public QueryRequest Request { get; }
        public JArray Target { get; }

        public QueryWorkItem(QueryRequest request, JArray target)
        {
            Request = request;
            Target = target;
        }
    }

    public class EntityWithAssociations
    {
        public Entity Entity;
        public Dictionary<string, List<Association>> Associations = new Dictionary<string, List<Association>>();
    }

    public class QueryEngine 
    {
        private readonly IGraphData _graph;        
        private readonly Dictionary<int, EntityWithAssociations> _cache = new Dictionary<int, EntityWithAssociations>();
        
        private List<QueryWorkItem> _work = new List<QueryWorkItem>();

        public int Calls { get; private set; }
        public double TimeFetching { get; private set; }

        public QueryEngine(IGraphData graph)
        {
            _graph = graph;
        }

        public async Task<JArray> Query(QueryRequest request)
        {
            var entities = new JArray();
            
            _work.Add(new QueryWorkItem(request, entities));

            while (_work.Count != 0)
            {
                await Flush();
            }

            return entities;
        }

        private async Task<T> CaptureTime<T>(Func<Task<T>> op)
        {
            Calls++;

            var started = new Stopwatch();
            started.Start();

            var result = await op();

            TimeFetching += started.Elapsed.TotalMilliseconds;
            started.Stop();

            return result;
        }

        private async Task FillCache(IEnumerable<int> entities)
        {
            var missing = entities.Except(_cache.Where(e => e.Value.Entity != null).Select(e => e.Key)).ToList();
            if (missing.Count == 0) return;
            
            foreach (var entity in await CaptureTime(() => _graph.GetEntities(missing)))
            {
                _cache.Get(entity.Id).Entity = entity;
            }
        }

        private async Task FillCache(IEnumerable<int> entities, string associationType)
        {
            var missing = entities.Except(_cache.Where(e => e.Value.Associations.ContainsKey(associationType))
                                                .Select(e => e.Key))
                                  .ToList();

            if (missing.Count == 0) return;

            Calls++;

            foreach (var association in await CaptureTime(() => _graph.GetAssociations(missing, associationType)))
            {
                _cache.Get(association.From).Associations.Get(associationType).Add(association);
            }
        }

        private async Task Flush()
        {
            // get all distinct entity IDs across waiting requests
            var entityIds = _work.SelectMany(w => w.Request.Entities).Distinct();
            
            await FillCache(entityIds);

            // get all (entity, assoc-type) combinations
            var joins = from w in _work
                        from e in w.Request.Entities
                        from j in w.Request.Template.Properties()
                        group e by j.Name;
                        
            foreach (var assoc in joins)
            {
                await FillCache(assoc, assoc.Key);
            }
        
            var work = _work;
            _work = new List<QueryWorkItem>();

            foreach (var item in work)
            {
                foreach (var e in item.Request.Entities)
                {
                    if (_cache.TryGetValue(e, out var cached))
                    {
                        var result = item.Target.FirstOrDefault(i => i["id"].Value<int>() == e) as JObject;
                        if (result == null)
                        {
                            result = new JObject();
                            item.Target.Add(result);
                        }
                        
                        result["type"] = cached.Entity.Type;
                        result["created"] = cached.Entity.Created.ToString("o");
                    
                        if (cached.Entity.Attributes != null)
                        {
                            result.Merge(cached.Entity.Attributes);
                        }
                    
                        foreach (var join in item.Request.Template.Properties())
                        {
                            if (cached.Associations.TryGetValue(join.Name, out var assocsFrom))
                            {
                                var assocResults = new JArray();
                                result[join.Name] = assocResults;

                                foreach (var assoc in assocsFrom)
                                {
                                    var assocResult = new JObject 
                                    {
                                        ["id"] = assoc.To,
                                        ["linked"] = assoc.Created
                                    };

                                    if (assoc.Attributes != null)
                                    {
                                        assocResult.Merge(assoc.Attributes);
                                    }

                                    assocResults.Add(assocResult);
                                }

                                _work.Add(new QueryWorkItem(
                                    new QueryRequest
                                    {
                                        Entities = assocsFrom.Select(a => a.To).ToList(),
                                        Template = join.Value as JObject ?? new JObject()
                                    },
                                    assocResults));
                            }
                        }
                    }
                }
            }
        }
    }
}