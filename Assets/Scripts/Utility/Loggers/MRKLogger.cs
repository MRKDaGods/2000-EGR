using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace MRK {
    public enum MRKLogType {
        Info,
        Warning,
        Error
    }

    public class MRKLogger {
        readonly static HashSet<IMRKLogger> ms_Loggers;

        public static bool ShowCallerNames { get; set; }

        static MRKLogger() {
            ms_Loggers = new HashSet<IMRKLogger>();
            ShowCallerNames = true;
        }

        static void Log(MRKLogType type, string msg, string callerPath) {
            if (ms_Loggers.Count == 0) {
                return;
            }
            
            string prefix = "[";
            switch (type) {
                case MRKLogType.Info:
                    prefix += "INFO";
                    break;

                case MRKLogType.Error:
                    prefix += "ERROR";
                    break;

                case MRKLogType.Warning:
                    prefix += "WARN";
                    break;
            }

            prefix += "]";

            string caller = ShowCallerNames ? $"[{Path.GetFileNameWithoutExtension(callerPath)}] "
                : string.Empty;

            foreach (IMRKLogger logger in ms_Loggers) {
                logger.Log(type, $"[{MRKTime.RelativeTime:hh\\:mm\\:ss\\.fff}] " +
                $"{prefix} " +
                $"{caller}" +
                $"{msg}" +
                $"{Environment.NewLine}");
            }
        }

        public static void LogInfo(string msg, [CallerFilePath] string path = null) {
            Log(MRKLogType.Info, msg, path);
        }

        public static void LogWarning(string msg, [CallerFilePath] string path = null) {
            Log(MRKLogType.Warning, msg, path);
        }

        public static void LogError(string msg, [CallerFilePath] string path = null) {
            Log(MRKLogType.Error, msg, path);
        }

        public static void Log(string msg, [CallerFilePath] string path = null) {
            Log(MRKLogType.Info, msg, path);
        }

        public static void AddLogger<T>() where T : IMRKLogger, new() {
            ms_Loggers.Add(new T());
        }
    }
}
