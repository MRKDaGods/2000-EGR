using System;
using System.Collections.Generic;

namespace MRK {
    public class ObjectPool<T> {
        Func<T> m_Instantiator;
        readonly List<T> m_FreeObjects;
        readonly List<T> m_ActiveObjects;
        readonly Dictionary<T, int> m_PoolIndex;
        int m_PoolCount;
        protected static ObjectPool<T> ms_DefaultPool;
        Action<T> m_OnFree;

        public int ActiveCount => m_ActiveObjects.Count;
        public List<T> ActiveObjects => m_ActiveObjects;
        public static ObjectPool<T> Default {
            get {
                if (ms_DefaultPool == null) {
                    ms_DefaultPool = new ObjectPool<T>(null);
                }

                return ms_DefaultPool;
            }
        }

        public ObjectPool(Func<T> instantiator, bool indexPool = false, Action<T> onFree = null) {
            m_Instantiator = instantiator;
            m_FreeObjects = new List<T>();
            m_ActiveObjects = new List<T>();

            if (indexPool)
                m_PoolIndex = new Dictionary<T, int>();

            m_OnFree = onFree;
        }

        public void Free(T obj, bool manual = false) {
            if (obj == null)
                return;

            if (m_OnFree != null) {
                m_OnFree(obj);
            }

            if (!manual) {
                m_ActiveObjects.Remove(obj);
            }

            m_FreeObjects.Add(obj);
        }

        public void Reserve(int count) {
            for (int i = 0; i < count; i++) {
                T rented = Rent(null, true);
                Free(rented);
            }
        }

        public T Rent(Reference<int> poolIndex = null, bool forceNew = false) {
            T use = default;

            if (m_FreeObjects.Count > 0 && !forceNew) {
                use = m_FreeObjects[0];
                m_FreeObjects.Remove(use);
            }
            else {
                use = m_Instantiator != null ? m_Instantiator() : Activator.CreateInstance<T>();

                if (m_PoolIndex != null) {
                    m_PoolIndex[use] = m_PoolCount++;
                }
            }

            if (poolIndex != null && m_PoolIndex != null)
                poolIndex.Value = m_PoolIndex[use];

            m_ActiveObjects.Add(use);
            return use;
        }

        public void FreeAll() {
            foreach (T obj in m_ActiveObjects) {
                Free(obj, true);
            }

            m_ActiveObjects.Clear();
        }
    }
}
