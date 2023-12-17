using System;
using System.Threading;

using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

using Eirshy.DSP.ReBuffer.AnyBeat;
using Eirshy.DSP.ReBuffer.NoRythhmn;
using Eirshy.DSP.ReBuffer.WithRythmn;

namespace Eirshy.DSP.ReBuffer {

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(RYTHMN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class ReBuffer : BaseUnityPlugin {
        public const string MODID = "ReBuffer";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "0.2.0";
        public const string NAME = "ReBuffer";

        const string RYTHMN_GUID = ROOT + "Rythmn";

        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));

        static internal ManualLogSource Logs { get; private set; }

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage("ReBuffer Active");
            DSP.ReBuffer.Config.Load(Config);
            HookAnyBeat();
            if(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(RYTHMN_GUID)) {
                HookWithRythmn();
            } else {
                HookNoRythmn();
            }
        }

        public static EEnabledComponents Enabled { get; internal set; } = EEnabledComponents._NONE;
        public static bool IsEnabled(EEnabledComponents component) => (Enabled & component) == component;

        //the following is provided in case you got here via reflection
        public static int ComponentsEnabled => (int)Enabled;
        public static bool IsComponentEnabled(int component) => (ComponentsEnabled & component) == component;


        private void HookAnyBeat() {
            if(IsEnabled(EEnabledComponents.AssemblerComponent)) AssemblerComponentPatcher.ApplyMe();
        }
        private void HookNoRythmn() {
            if(IsEnabled(EEnabledComponents.LabComponent)) LabComponentPatcher.ApplyMe();
        }
        private void HookWithRythmn() {
            Logs.LogMessage("Taking advantage of having Rythmn!");
            if(IsEnabled(EEnabledComponents.LabComponent)) {
                LabComponentPatcher.ApplyMe();
                if(false) {//disabled outright for now, thread safety was backwards lol
                    if(DSP.ReBuffer.Config.CollapseLabTowers) {
                        Enabled |= EEnabledComponents.LabDancers;
                        LabComponentDancer.ApplyMe();
                    } else LabComponentPatcher.ApplyMe();
                }
            }
        }
    }
}
