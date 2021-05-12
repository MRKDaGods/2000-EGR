using System;
using System.Collections.Generic;

namespace MRK {
    public class ObjectPool<T> {
        Func<T> m_Instantiator;
        readonly List<T> m_FreeObjects;
        readonly List<T> m_ActiveObjects;

        public int ActiveCount => m_ActiveObjects.Count;

        public ObjectPool(Func<T> instantiator) {
            m_Instantiator = instantiator;
            m_FreeObjects = new List<T>();
            m_ActiveObjects = new List<T>();
        }

        public void Free(T obj) {
            m_ActiveObjects.Remove(obj);
            m_FreeObjects.Add(obj);
        }

        public T Rent() {
            T use = default;
            if (m_FreeObjects.Count > 0) {
                use = m_FreeObjects[0];
                m_FreeObjects.Remove(use);
            }
            else {
                use = m_Instantiator != null ? m_Instantiator() : Activator.CreateInstance<T>();
            }

            m_ActiveObjects.Add(use);

            EGRMain.Log($"Renting -> {m_ActiveObjects.Count} - {m_FreeObjects.Count}");
            return use;
        }
    }
}
