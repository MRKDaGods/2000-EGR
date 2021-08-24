using System;

namespace MRK {
    public enum LogType {
        None,
        Info,
        Warning,
        Error
    }

    public interface IEGRLogger {
        void Log(DateTime timestamp, LogType type, string msg);
    }
}