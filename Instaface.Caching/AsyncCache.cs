namespace Instaface.Caching
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    public abstract class AsyncCache<TItem> where TItem : class
    {        
        private readonly object _locker = new object();

        private readonly Dictionary<int, Task<TItem>> _items = new Dictionary<int, Task<TItem>>();

        protected abstract Task<IReadOnlyCollection<TItem>> GetItems(IEnumerable<int> ids);        
        protected abstract int GetId(TItem item);

        public Task<IReadOnlyCollection<TItem>> Get(IEnumerable<int> idSeq, Action<bool, IReadOnlyCollection<int>> raiseEvent)
        {
            var ids = idSeq as IReadOnlyCollection<int> ?? idSeq.ToList();
            if (ids.Count == 0) return Task.FromResult(new TItem[0] as IReadOnlyCollection<TItem>);

            lock (_locker)
            {
                var available = ids.Where(i => _items.ContainsKey(i)).ToList();
                var missing = ids.Except(available).ToList();

                if (missing.Count != 0)
                {
                    raiseEvent(false, missing);

                    var adding = GetEntitiesById(missing);

                    foreach (var id in missing)
                    {
                        _items[id] = Fetch(adding, id);
                    }
                }

                if (available.Count != 0)
                {
                    raiseEvent(true, available);
                }

                return Complete(ids.Select(id => _items[id]));
            }
        }

        public void Delete(int id)
        {
            lock (_locker)
            {
                _items.Remove(id);
            }
        }

        private static async Task<IReadOnlyCollection<TItem>> Complete(IEnumerable<Task<TItem>> tasks)
        {
            return (await Task.WhenAll(tasks)).Where(i => i != null).ToList();
        }

        private static async Task<TItem> Fetch(Task<IDictionary<int, TItem>> source, int id)
        {
            return (await source).TryGetValue(id, out var entity) ? entity : null;
        }

        private async Task<IDictionary<int, TItem>> GetEntitiesById(IReadOnlyCollection<int> ids)
        {
            var faulted = true;

            try
            {
                var entities = await GetItems(ids);
                faulted = false;
                return entities.ToDictionary(GetId);
            }
            finally
            {
                if (faulted)
                {
                    lock (_locker)
                    {
                        foreach (var id in ids)
                        {
                            _items.Remove(id);
                        }
                    }
                }
            }
        }        
    }
}