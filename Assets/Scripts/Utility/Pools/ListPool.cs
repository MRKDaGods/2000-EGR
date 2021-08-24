using System;
using System.Collections.Generic;

namespace MRK {
    public class ListPool<T> : ObjectPool<List<T>> {
        public static new ObjectPool<List<T>> Default {
            get {
                if (ms_DefaultPool == null) {
                    ms_DefaultPool = new ListPool<T>(null);
                }

                return ms_DefaultPool;
            }
        }

        public ListPool(Func<List<T>> instantiator, bool indexPool = false) : base(instantiator, indexPool, OnFree) {
        }

        static void OnFree(List<T> obj) {
            if (obj.Count > 0) {
                obj.Clear();
            }
        }
    }
}
