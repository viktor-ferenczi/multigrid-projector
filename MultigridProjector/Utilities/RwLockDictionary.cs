using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MultigridProjector.Utilities
{
    public class RwLockDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private readonly RwLock _lock = new RwLock();

        public RwLockDictionary()
        {
        }

        public RwLockDictionary(int capacity) : base(capacity)
        {
        }

        public RwLockDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
        {
        }

        public RwLockDictionary(int capacity, IEqualityComparer<TKey> comparer) : base(capacity, comparer)
        {
        }

        public RwLockDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }

        public RwLockDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer) : base(dictionary, comparer)
        {
        }

        protected RwLockDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public RwLock.Reader Read()
        {
            return _lock.Read();
        }

        public RwLock.Writer Write()
        {
            return _lock.Write();
        }
    }
}