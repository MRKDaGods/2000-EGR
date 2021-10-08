using UnityEngine;

namespace MRK {
    public class UnityLogger : IMRKLogger {
        public void Log(MRKLogType type, string msg) {
            switch (type) {
                case MRKLogType.Info:
                    Debug.Log(msg);
                    break;

                case MRKLogType.Warning:
                    Debug.LogWarning(msg);
                    break;

                case MRKLogType.Error:
                    Debug.LogError(msg);
                    break;
            }
        }
    }
}