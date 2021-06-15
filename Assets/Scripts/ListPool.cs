using System;
using System.Collections.Generic;

namespace MRK {
    public class ListPool<T> : ObjectPool<List<T>> {
        public ListPool(Func<List<T>> instantiator, bool indexPool = false) : base(instantiator, indexPool) {
        }

        public override void Free(List<T> obj) {
            if (obj.Count > 0) {
                obj.Clear();
            }

            base.Free(obj);
        }
    }
}
