using System;
using System.Collections.Generic;

namespace Instaface
{    
    public static class CollectionExtensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> target, TKey key, Func<TValue> creator)
        {
            if (!target.TryGetValue(key, out var entry))
            {
                target[key] = entry = creator();
            }

            return entry;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> target, TKey key) where TValue : new()
        {
            return target.Get(key, () => new TValue());        
        }
    }
}
