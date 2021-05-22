using System.Collections;
using UnityEngine;

namespace MRK {
    public class CoroutineRunner : MonoBehaviour {
        class Lock {
            public volatile int Count;
        }

        readonly Lock m_Lock;

        public int Count => m_Lock.Count;

        public CoroutineRunner() {
            m_Lock = new Lock();
        }

        IEnumerator _Run(IEnumerator routine) {
            lock (m_Lock) {
                m_Lock.Count++;
            }

            yield return routine;

            lock (m_Lock) {
                m_Lock.Count--;
            }
        }

        public void Run(IEnumerator coroutine) {
            StartCoroutine(_Run(coroutine));
        }

        public void StopAll() {
            StopAllCoroutines();
            m_Lock.Count = 0;
        }
    }
}
