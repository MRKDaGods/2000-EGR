using System.Collections.Generic;

namespace MRK
{
    public class Registry<K, V> : Dictionary<K, V>
    {
        private static Registry<K, V> _global;

        public static Registry<K, V> Global
        {
            get
            {
                return _global ??= new Registry<K, V>();
            }
        }
    }
}
