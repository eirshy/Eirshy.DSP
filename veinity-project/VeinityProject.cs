using System;
using System.Threading;

using BepInEx;
using BepInEx.Logging;

using HarmonyLib;


namespace Eirshy.DSP.VeinityProject {

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(SMELTERMINER, BepInDependency.DependencyFlags.SoftDependency)]
    public class VeinityProject : BaseUnityPlugin {
        public const string MODID = "VeinityProject";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "0.1.6.0";
        public const string NAME = "VeinityProject";

        const string SMELTERMINER = "Gnimaerd.DSP.plugin.SmelterMiner";
        internal static readonly Lazy<bool> SmelterMiner_Exists = new Lazy<bool>(
            ()=>BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(SMELTERMINER)
            , LazyThreadSafetyMode.PublicationOnly
        );


        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));

        static internal ManualLogSource Logs { get; private set; }

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage("VeinityProject powdering up!");
            DSP.VeinityProject.Config.Load(Config);
            SmelterMinerCompat.SetUp();
            MinerComponentPatcher.SetUp();
        }
    }
}
