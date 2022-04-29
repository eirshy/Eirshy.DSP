using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Logging;

namespace Eirshy.DSP.Rythmn.Logging {

    /// <summary>
    /// Interface for our (internal, standardized) logging.
    /// </summary>
    public interface ILogProvider {
        /// <summary>
        /// Direct access to the BepIn logger, if one was provided.
        /// Should be null if one was not.
        /// </summary>
        ManualLogSource BepIn { get; }
        /// <summary>
        /// Logs the given messsage.
        /// </summary>
        void Log(string msg);
        /// <summary>
        /// Logs that the given verse is calling the given stanza.
        /// </summary>
        void LogStanza(Type verse, string stanza);
        /// <summary>
        /// Logs that we've encountered a fatal error.
        /// </summary>
        void LogFatal(Exception ex);
        /// <summary>
        /// If the current logging method uses a log buffer, flushes it to whatever output source it uses.
        /// </summary>
        void FlushLogBuffer();
    }
}
