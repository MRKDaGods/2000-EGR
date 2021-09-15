using System;
using System.Collections.Generic;

namespace MRK {
    public class HashSetPool<T> : ObjectPool<HashSet<T>> {
        public static new ObjectPool<HashSet<T>> Default {
            get {
                if (ms_DefaultPool == null) {
                    ms_DefaultPool = new HashSetPool<T>(null);
                }

                return ms_DefaultPool;
            }
        }

        public HashSetPool(Func<HashSet<T>> instantiator, bool indexPool = false) : base(instantiator, indexPool, OnFree) {
        }

        static void OnFree(HashSet<T> obj) {
            if (obj.Count > 0) {
                obj.Clear();
            }
        }
    }
}
