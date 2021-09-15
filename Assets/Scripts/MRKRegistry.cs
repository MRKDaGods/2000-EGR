using System.Collections.Generic;

namespace MRK {
    public class MRKRegistry<K, V> : Dictionary<K, V> {
        static MRKRegistry<K, V> ms_Global;

        public static MRKRegistry<K, V> Global => ms_Global ??= new MRKRegistry<K, V>();
    }
}
