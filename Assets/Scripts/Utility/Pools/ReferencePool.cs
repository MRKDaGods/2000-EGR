using System;

namespace MRK {
    public class ReferencePool<T> : ObjectPool<Reference<T>> {
        public static new ObjectPool<Reference<T>> Default {
            get {
                if (ms_DefaultPool == null) {
                    ms_DefaultPool = new ReferencePool<T>(null);
                }

                return ms_DefaultPool;
            }
        }

        public ReferencePool(Func<Reference<T>> instantiator, bool indexPool = false) : base(instantiator, indexPool, OnFree) {
        }

        static void OnFree(Reference<T> obj) {
            obj.Value = default;
        }
    }
}
