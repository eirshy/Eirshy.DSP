using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BepInEx.Logging;

namespace Eirshy.DSP.Rythmn.Logging {
    /// <summary>
    /// The "No Logging" LogProvider singleton, as mapping everything to null/noop doesn't really have multiple ways of doing it.
    /// </summary>
    public sealed class DisabledLogger : ILogProvider {
        static readonly Lazy<DisabledLogger> _inst = new Lazy<DisabledLogger>(() => new DisabledLogger(), LazyThreadSafetyMode.PublicationOnly);
        public static DisabledLogger Instance => _inst.Value;
        private DisabledLogger() { }
        public ManualLogSource BepIn => null;
        public void Log(string msg) { }
        public void LogStanza(Type verse, string name) { }
        public void LogRecoverable(string msg, Exception ex) { }
        public void LogFatal(Exception ex) { }
        public void FlushLogBuffer() { }
    }
}
