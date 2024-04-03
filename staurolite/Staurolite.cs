using System;
using System.Runtime.CompilerServices;
using System.Threading;

using BepInEx;
using BepInEx.Logging;

using HarmonyLib;


namespace Eirshy.DSP.Staurolite {

    [BepInPlugin(GUID, NAME, VERSION)]
    public class Staurolite : BaseUnityPlugin {
        public const string MODID = "Staurolite";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "1.0.0";
        public const string NAME = "Staurolite";

        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new(() => new Harmony(GUID));

        static internal ManualLogSource Logs { get; private set; }

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage("Staurolite injectors green, splitters primed!");
            DSP.Staurolite.Config.Load(Config);
            Harmony.PatchAll(typeof(StauroliteJet));
        }

    }
}
