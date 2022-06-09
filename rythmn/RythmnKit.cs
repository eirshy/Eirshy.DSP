using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using Eirshy.DSP.Rythmn.Logging;
using Eirshy.DSP.Rythmn.Enums;

namespace Eirshy.DSP.Rythmn {
    [BepInPlugin(GUID, NAME, VERSION)]
    public class RythmnKit : BaseUnityPlugin {
        public const string MODID = "Rythmn";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "1.0.0.0";
        public const string NAME = "Rythmn Kit";

        internal const string OTHER_MODS_LDBTOOL = "me.xiaoye97.plugin.Dyson.LDBTool";
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));
        internal static Harmony Harmony => _harmony.Value;
        public static ILogProvider LogProvider = null;

        private void Awake() {
            LoadLogger();
            Hello();
            StaticBeat.SetUp(Config);
        }


        static readonly Regex _cfgsplitter = new Regex(@"[^a-zA-Z0-9._-]+");
        const string _cfgsplitter_glue = " ";

        /// <summary>
        /// Splits on regex: <c>[^a-zA-Z0-9._-]+</c>
        /// </summary>
        internal static string[] ConfigSplit(string s) {
            if(string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            var ret = _cfgsplitter.Split(s).Where(s => s != "").ToArray();
            if(ret.Length == 0
                || (ret.Length == 1 && ret[0] == "")
            ) {
                return Array.Empty<string>();
            }
            return ret;
        }
        /// <summary>
        /// Joins sa using the standard separator
        /// </summary>
        internal static string ConfigJoin(string[] sa) => string.Join(_cfgsplitter_glue, sa);
        /// <summary>
        /// Joins ies using the standard separator
        /// </summary>
        internal static string ConfigJoin(IEnumerable<string> ies) => string.Join(_cfgsplitter_glue, ies);
        /// <summary>
        /// Joins iet using the standard separator and T.ToString();
        /// </summary>
        internal static string ConfigJoin<T>(IEnumerable<T> iet) => string.Join(_cfgsplitter_glue, iet.Select(t=>t.ToString()));


        #region Set up default LogProvider
        //Set up to be switchable between BepIn and a manual buffered file log.
        //If you've got the BepIn console open you don't really need to use a file log.
        /** /
        static readonly string FILE = $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}\\game hacking\\DysonSphere\\modlog.txt";
        static ConcurrentStack<string> _log = new ConcurrentStack<string>();
        static ConcurrentStack<string> _swap = new ConcurrentStack<string>();
        const int AUTO_FLUSH_AT = 10;
        static int flushing = 0;
        static bool flushing_mine => Interlocked.Exchange(ref flushing, -1) == 0;
        static void flushing_release() => flushing = 0;
        static void Log(string text) {
            _log.Push(text);
            if(_log.Count > AUTO_FLUSH_AT) FlushLog();
        }
        static void FlushLog() {
            if(flushing_mine){
                try {
                    var toEmpty = _log;
                    _log = _swap;
                    List<string> popped = new List<string>(toEmpty.Count);
                    while(toEmpty.TryPop(out var pop)) popped.Add(pop);
                    popped.Reverse();
                    System.IO.File.AppendAllLines(FILE, popped);
                    _swap = toEmpty;
                    flushing_release();
                } catch(Exception ex) {
                    System.IO.File.AppendAllText(FILE, $"Flush Fatal\n{LoggingFormatters.LogFatal(ex)}");
                }
            }
        }
        void Hello() {
            var bd = typeof(RythmnKit).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            if(System.IO.File.ReadAllLines(FILE).Length > 200) System.IO.File.Delete(FILE);
            Log($"\n\nRythmn Toolkit -- {bd}");
            FlushLog();
        }

        void LoadLogger() => LogProvider = new CustomLogger(Log, FlushLog);
        /*/

        void Hello() {
            Logger.LogMessage($"Rythmn Toolkit v{VERSION} -- Drums at the ready!");
        }
        void LoadLogger() => LogProvider = (BepInManualLogger)Logger;
        /**/

        #endregion
        #region Static Beat hoists:

        public static void AddSetup(Action todo, ESetupPhase phase) => StaticBeat.AddSetup(todo, phase);

        public static void AddLoad_PreVisitor(Action<GameData> todo) => StaticBeat.AddLoad_PreVisitor(todo);
        public static void AddLoad_Visitor(Action<EntityRef> todo) => StaticBeat.AddLoad_Visitor(todo);
        public static void AddLoad_PostVisitor(Action<GameData> todo) => StaticBeat.AddLoad_PostVisitor(todo);

        public static void AddSaveClean_PreVisitor(Action<GameData> todo) => StaticBeat.AddSaveClean_PreVisitor(todo);
        public static void AddSaveClean_Visitor(Action<EntityRef> todo) => StaticBeat.AddSaveClean_Visitor(todo);
        public static void AddSaveClean_PostVisitor(Action<GameData> todo) => StaticBeat.AddSaveClean_PostVisitor(todo);

        public static void AddSaveRestore_PreVisitor(Action<GameData> todo) => StaticBeat.AddSaveRestore_PreVisitor(todo);
        public static void AddSaveRestore_Visitor(Action<EntityRef> todo) => StaticBeat.AddSaveRestore_Visitor(todo);
        public static void AddSaveRestore_PostVisitor(Action<GameData> todo) => StaticBeat.AddSaveRestore_PostVisitor(todo);


        public static EOnLoadPrefabSync WillSync => StaticBeat.WillSync;
        public static void RequestPrefabSync(EOnLoadPrefabSync syncs) => StaticBeat.RequestPrefabSync(syncs);

        #endregion
    }
}
