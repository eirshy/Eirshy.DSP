using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Logging;

namespace Eirshy.DSP.Rythmn.Logging {

    /// <summary>
    /// A pre-implemented generic logger with custom implementation and standard formatting
    /// </summary>
    public sealed class CustomLogger : ILogProvider {
        readonly Action _flush;
        readonly Action<string> _log;
        readonly Action<Type, string> _logStanza;
        readonly Action<string, Exception> _logRecoverable;
        readonly Action<Exception> _logFatal;

        /// <param name="log">
        /// Required, writes to our log.
        /// </param>
        /// <param name="logFlush">
        /// Optional. If your log source is buffered, place its flush command here.
        /// </param>
        /// <param name="logStanza">
        /// Optional. If not provided, the default formatting will be used, using the <c>log</c> param as the provider.
        /// </param>
        /// <param name="logFatal">
        /// Optional. If not provided, the default formatting will be used, using the <c>log</c> param as the provider.
        /// </param>
        /// <param name="logRecoverable">
        /// Optional. If not provided, the default formatting will be used, using the <c>log</c> param as the provider.
        /// </param>
        public CustomLogger(
            Action<string> log,
            Action logFlush = null,
            Action<Type, string> logStanza = null,
            Action<string, Exception> logRecoverable = null,
            Action<Exception> logFatal = null
        ) {
            _log = log;
            _logStanza = logStanza;
            _logRecoverable = logRecoverable;
            _logFatal = logFatal;
            _flush = logFlush;
        }

        public ManualLogSource BepIn => null;
        public void Log(string msg) => _log(LoggingFormatters.Log(msg));
        public void LogStanza(Type verse, string name) {
            if(_logStanza is null) _log(LoggingFormatters.LogStanza(verse, name));
            else _logStanza(verse, name);
        }
        public void LogRecoverable(string msg, Exception ex = null) {
            if(_logRecoverable is null) _log(LoggingFormatters.LogRecoverable(msg, ex));
            else _logRecoverable(msg, ex);
        }

        public void LogFatal(Exception ex) {
            if(_logFatal is null) _log(LoggingFormatters.LogFatal(ex));
            else _logFatal(ex);
        }
        public void FlushLogBuffer() => _flush?.Invoke();
    }


}
