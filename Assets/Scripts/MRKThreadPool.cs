using System;
using System.Collections.Generic;
using System.Threading;

namespace MRK {
    public class MRKThreadPool {
        const int INACTIVITY_TIMER = 5000;

        int m_Interval;
        bool m_Running;
        Thread m_Thread;
        readonly Queue<Action> m_TaskQueue;
        DateTime? m_InactivityStartTime;

        public static MRKThreadPool Global => EGRMain.Instance.GlobalThreadPool;

        public MRKThreadPool(int interval) {
            m_Interval = interval;
            m_TaskQueue = new Queue<Action>();
        }

        void ThreadLoop() {
            while (m_Running) {
                if (m_TaskQueue.Count > 0) {
                    m_InactivityStartTime = null;

                    Action act;
                    //quick lock
                    lock (m_TaskQueue) {
                        act = m_TaskQueue.Dequeue();
                    }

                    act.Invoke();
                }
                else {
                    if (!m_InactivityStartTime.HasValue)
                        m_InactivityStartTime = DateTime.Now;

                    if ((DateTime.Now - m_InactivityStartTime.Value).TotalMilliseconds > INACTIVITY_TIMER) {
                        m_InactivityStartTime = null;
                        Terminate();
                    }
                }

                Thread.Sleep(m_Interval);
            }
        }

        public void QueueTask(Action action) {
            //please avoid deadlock
            lock (m_TaskQueue) {
                m_TaskQueue.Enqueue(action);
            }

            if (!m_Running)
                Start();
        }

        void Start() {
            m_Running = true;
            m_Thread = new Thread(ThreadLoop);
            m_Thread.Start();
        }

        public void Terminate() {
            m_Running = false;
            m_Thread.Abort();
        }
    }
}
