using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK
{
    public class Runnable : MonoBehaviour
    {
        private class Lock
        {
            public volatile int Count;
        }

        private class RunnableAction
        {
            public bool IsPersistent;
            public Action Action;
        }

        private readonly Lock _lock;
        private readonly List<RunnableAction> _mainThreadQueue;

        public int Count
        {
            get
            {
                return _lock.Count;
            }
        }

        public bool IsCalledByRunnable
        {
            get; private set;
        }

        public Runnable()
        {
            _lock = new Lock();
            _mainThreadQueue = new List<RunnableAction>();
        }

        private IEnumerator _Run(IEnumerator routine)
        {
            lock (_lock)
            {
                _lock.Count++;
            }

            yield return routine;

            lock (_lock)
            {
                _lock.Count--;
            }
        }

        public void Run(IEnumerator coroutine)
        {
            StartCoroutine(_Run(coroutine));
        }

        private IEnumerator _RunLaterFrames(Action act, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
            }

            act?.Invoke();
        }

        public void RunLaterFrames(Action act, int frames)
        {
            StartCoroutine(_RunLaterFrames(act, frames));
        }

        private IEnumerator _RunLater(Action act, float time)
        {
            lock (_lock)
            {
                _lock.Count++;
            }

            yield return new WaitForSeconds(time);
            act?.Invoke();

            lock (_lock)
            {
                _lock.Count--;
            }
        }

        public void RunLater(Action act, float time)
        {
            StartCoroutine(_RunLater(act, time));
        }

        public void RunOnMainThread(Action action, bool persistent = false)
        {
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Add(new RunnableAction
                {
                    Action = action,
                    IsPersistent = persistent
                });
            }
        }

        private void Update()
        {
            if (_mainThreadQueue.Count > 0)
            {
                lock (_mainThreadQueue)
                {
                    IsCalledByRunnable = true;

                    for (int i = _mainThreadQueue.Count - 1; i > -1; i--)
                    {
                        RunnableAction action = _mainThreadQueue[i];
                        action.Action();

                        if (!action.IsPersistent)
                        {
                            _mainThreadQueue.RemoveAt(i);
                        }
                    }

                    IsCalledByRunnable = false;
                }
            }
        }

        public void StopAll()
        {
            StopAllCoroutines();
            _lock.Count = 0;
        }
    }
}
