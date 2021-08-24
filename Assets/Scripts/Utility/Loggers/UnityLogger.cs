using System;
using UnityEngine;

namespace MRK {
    public class UnityLogger : IEGRLogger {
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