using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.GameSystems.Conveyors;

namespace MultigridProjector.Extensions
{
    public static class DictionaryExtensions
    {
        public static void Update<TK, TV>(this Dictionary<TK, TV> dict, Dictionary<TK, TV> other)
        {
            var removeKeys = dict.Keys.Where(key => !other.ContainsKey(key)).ToList();
            foreach (var key in removeKeys) dict.Remove(key);
            foreach (var (key, value) in other) dict[key] = value;
        }
    }
}