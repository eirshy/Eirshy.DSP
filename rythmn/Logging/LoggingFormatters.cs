using System;

namespace Eirshy.DSP.Rythmn.Logging {
    /// <summary>
    /// Formatting helpers for if you want to have the same "look and feel" of our default logging practices.
    /// </summary>
    public static class LoggingFormatters {
        public static string Log(string msg) => $"...{msg}";
        public static string LogStanza(Type verse, string name) => $"Running {verse.Name}.{name}...";
        public static string LogRecoverable(string msg, Exception ex) {
            string inner = ex?.InnerException is null ? "" : $" (inner: {ex.InnerException.GetType().Name})";
            return (msg is null ? "" : $"ERROR: {msg}") +
                (msg != null && ex != null ? "\n" : "") +
                (ex is null ? "" :
                    $"CAUGHT: {ex.GetType().Name}" +
                    $"\n--in {ex.TargetSite.Name}{inner}" +
                    $"\n--{ex.Message}" +
                    $"\n-:::" +
                    $"\n{ex.StackTrace}"
                )
            ;
        }
        public static string LogFatal(Exception ex) {
            string inner = ex.InnerException is null ? "" : $" (inner: {ex.InnerException.GetType().Name})";
            return $"THREW: {ex.GetType().Name}" +
                $"\n--in {ex.TargetSite.Name}{inner}" +
                $"\n--{ex.Message}" +
                $"\n-:::" +
                $"\n{ex.StackTrace}"
            ;
        }
    }
}
