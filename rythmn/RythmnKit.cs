using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

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


        #region Set up default LogProvider 
#if DEBUG
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
#else
        void Hello() {
            Logger.LogMessage($"Rythmn Toolkit v{VERSION} -- Drums at the ready!");
        }
        void LoadLogger() => LogProvider = (BepInManualLogger)Logger;
#endif
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
