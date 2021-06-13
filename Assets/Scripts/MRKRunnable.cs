using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
    public class MRKRunnable : MonoBehaviour {
        class Lock {
            public volatile int Count;
        }

        readonly Lock m_Lock;
        readonly List<Action> m_MainThreadQueue;

        public int Count => m_Lock.Count;

        public MRKRunnable() {
            m_Lock = new Lock();
            m_MainThreadQueue = new List<Action>();
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

        IEnumerator _RunLater(Action act, float time) {
            lock (m_Lock) {
                m_Lock.Count++;
            }

            yield return new WaitForSeconds(time);
            act?.Invoke();

            lock (m_Lock) {
                m_Lock.Count--;
            }
        }

        public void RunLater(Action act, float time) {
            StartCoroutine(_RunLater(act, time));
        }

        public void RunOnMainThread(Action action) {
            lock (m_MainThreadQueue) {
                m_MainThreadQueue.Add(action);
            }
        }

        void Update() {
            if (m_MainThreadQueue.Count > 0) {
                lock (m_MainThreadQueue) {
                    foreach (Action action in m_MainThreadQueue) {
                        action();
                    }

                    m_MainThreadQueue.Clear();
                }
            }
        }

        public void StopAll() {
            StopAllCoroutines();
            m_Lock.Count = 0;
        }
    }
}
