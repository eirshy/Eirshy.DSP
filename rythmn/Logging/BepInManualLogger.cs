using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Logging;


namespace Eirshy.DSP.Rythmn.Logging {

    /// <summary>
    /// A pre-implemented wrapper for the BepInEx ManualLogSource, since 9/10 this is what you should be using.
    /// </summary>
    public sealed class BepInManualLogger : ILogProvider {
        public ManualLogSource BepIn { get; }

        /// <param name="bepIn">
        /// If creating this in your plugin's root class, should be simply <c>Logger</c>
        /// </param>
        public BepInManualLogger(ManualLogSource bepIn) => BepIn = bepIn;

        public void Log(string msg) => BepIn.LogMessage(LoggingFormatters.Log(msg));
        public void LogStanza(Type verse, string name) => BepIn.LogMessage(LoggingFormatters.LogStanza(verse, name));
        public void LogFatal(Exception ex) => BepIn.LogFatal(LoggingFormatters.LogFatal(ex));

        void ILogProvider.FlushLogBuffer() { }

        //implicit don't work, and you can't declare an interface caster for the obvious reason of "that's not how interfaces work".
        //Could get around this by dropping the interface and using another abstract, but...
        //  This is literally an instance where an interface is *propper*, since it allows us to use
        //  any other custom logger by just having the modder slap our interface onto their logger and implement-explicit.
        public static explicit operator BepInManualLogger(ManualLogSource bepin) => new BepInManualLogger(bepin);
    }

}
