using System;
using UnityEngine;

namespace MRK {
    public enum LogType {
        None,
        Info,
        Warning,
        Error
    }

    public interface EGRLogger {
        void Log(DateTime timestamp, LogType type, string msg);
    }

    public class UnityLogger : EGRLogger {
        public void Log(DateTime timestamp, LogType type, string msg) {
            switch (type) {

                case LogType.Info:
                    Debug.Log(msg);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(msg);
                    break;

                case LogType.Error:
                    Debug.LogError(msg);
                    break;

            }
        }
    }
}