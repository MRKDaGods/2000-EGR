using System;
using System.Collections.Generic;
using System.Threading;

namespace MRK.Threading
{
    public class ThreadPool
    {
        private const int InactivityTimer = 5000;

        private int _interval;
        private bool _running;
        private Thread _thread;
        private readonly Queue<Action> _taskQueue;
        private DateTime? _inactivityStartTime;

        public static ThreadPool Global
        {
            get
            {
                return EGR.Instance.GlobalThreadPool;
            }
        }

        public ThreadPool(int interval)
        {
            _interval = interval;
            _taskQueue = new Queue<Action>();
        }

        private void ThreadLoop()
        {
            while (_running)
            {
                if (_taskQueue.Count > 0)
                {
                    _inactivityStartTime = null;

                    Action act;
                    //quick lock
                    lock (_taskQueue)
                    {
                        act = _taskQueue.Dequeue();
                    }

                    act.Invoke();
                }
                else
                {
                    if (!_inactivityStartTime.HasValue)
                    {
                        _inactivityStartTime = DateTime.Now;
                    }

                    if ((DateTime.Now - _inactivityStartTime.Value).TotalMilliseconds > InactivityTimer)
                    {
                        _inactivityStartTime = null;
                        Terminate();
                    }
                }

                Thread.Sleep(_interval);
            }
        }

        public void QueueTask(Action action)
        {
            //please avoid deadlock
            lock (_taskQueue)
            {
                _taskQueue.Enqueue(action);
            }

            if (!_running)
            {
                Start();
            }
        }

        private void Start()
        {
            _running = true;
            _thread = new Thread(ThreadLoop);
            _thread.Start();
        }

        public void Terminate()
        {
            _running = false;
            _thread.Abort();
        }
    }
}
