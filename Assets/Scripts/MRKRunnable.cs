using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
    public class MRKRunnable : MonoBehaviour {
        class Lock {
            public volatile int Count;
        }

        class RunnableAction {
            public bool IsPersistent;
            public Action Action;
        }

        readonly Lock m_Lock;
        readonly List<RunnableAction> m_MainThreadQueue;

        public int Count => m_Lock.Count;
        public bool IsCalledByRunnable { get; private set; }

        public MRKRunnable() {
            m_Lock = new Lock();
            m_MainThreadQueue = new List<RunnableAction>();
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

        IEnumerator _RunLaterFrames(Action act, int frames) {
            for (int i = 0; i < frames; i++)
                yield return new WaitForEndOfFrame();

            act?.Invoke();
        }

        public void RunLaterFrames(Action act, int frames) {
            StartCoroutine(_RunLaterFrames(act, frames));
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

        public void RunOnMainThread(Action action, bool persistent = false) {
            lock (m_MainThreadQueue) {
                m_MainThreadQueue.Add(new RunnableAction {
                    Action = action,
                    IsPersistent = persistent
                });
            }
        }

        void Update() {
            if (m_MainThreadQueue.Count > 0) {
                lock (m_MainThreadQueue) {
                    IsCalledByRunnable = true;

                    for (int i = m_MainThreadQueue.Count - 1; i > -1; i--) {
                        RunnableAction action = m_MainThreadQueue[i];
                        action.Action();

                        if (!action.IsPersistent) {
                            m_MainThreadQueue.RemoveAt(i);
                        }
                    }

                    IsCalledByRunnable = false;
                }
            }
        }

        public void StopAll() {
            StopAllCoroutines();
            m_Lock.Count = 0;
        }
    }
}
