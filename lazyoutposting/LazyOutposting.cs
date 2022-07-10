using System;
using System.Threading;
using System.Reflection;

using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

using UnityEngine;

using HarmonyLib;

using Eirshy.DSP.LazyOutposting.Components;

namespace Eirshy.DSP.LazyOutposting {

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(OTHERMOD_VEINITYPROJECT, BepInDependency.DependencyFlags.SoftDependency)]
    public class LazyOutposting : BaseUnityPlugin {
        public const string MODID = "LazyOutposting";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "1.1.0.0";
        public const string NAME = "Lazy Outposting";

        internal const string OTHERMOD_BPTWEEKS = "org.kremnev8.plugin.BlueprintTweaks";
        internal const string OTHERMOD_VEINITYPROJECT = ROOT + "VeinityProject";

        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));

        static internal ManualLogSource Logs { get; private set; }

        internal static bool EnableDwarvenCommute { get; private set; }
        internal static bool EnableVaporCollection { get; private set; }
        internal static bool GiveDwarvesBuckets { get; private set; }

        internal static bool VeinityProjectExists { get; private set; }

        internal static class SoftDepTricks {
            public static bool OilMiners { get; private set; } = false;
            public static bool OceanMiners { get; private set; } = false;

            public static void Initialzie() {
                if(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(OTHERMOD_VEINITYPROJECT)) {
                    _load_veinityProject();
                }
            }
            private static void _load_veinityProject() {
                OilMiners = GiveDwarvesBuckets;//if the mod's enabled 
            }
        }

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage($"Lazy Outposting - For anywhere but Hoxxes IV!");
            SoftDepTricks.Initialzie();

            //config
            const string HDR = nameof(LazyOutposting);
            const string REQ_OTHER_MOD = "Requires the mod VeinityProject, otherwise will be ignored.";

            EnableDwarvenCommute = Config.Bind<bool>(HDR, nameof(EnableDwarvenCommute), true, new ConfigDescription(
                "If we should enable planet-wide mining."
            )).Value;
            EnableVaporCollection = Config.Bind<bool>(HDR, nameof(EnableVaporCollection), true, new ConfigDescription(
                "If we should enable ocean collection regardless of said ocean being accessible."
            )).Value;

            GiveDwarvesBuckets = Config.Bind<bool>(HDR, nameof(GiveDwarvesBuckets), true, new ConfigDescription(
                $"{REQ_OTHER_MOD}" +
                $"\nIf we should allow miners to collect oil."
            )).Value;



            if(EnableDwarvenCommute) DwarvenCommute.SetUp();
            if(EnableVaporCollection) Harmony.PatchAll(typeof(VaporCollection));
        }

        /// <summary>
        /// Cyclic "active key"
        /// </summary>
        public static uint OnKey = 0;
        private void Update() {
            if(Input.GetKeyDown(KeyCode.PageDown)) _ = unchecked(OnKey--);
            if(Input.GetKeyDown(KeyCode.PageUp)) _ = unchecked(OnKey++);
        }

        static Lazy<FieldInfo[]> _cloner = new Lazy<FieldInfo[]>(() =>
            typeof(PrefabDesc).GetFields(BindingFlags.Public | BindingFlags.Instance)
        , LazyThreadSafetyMode.PublicationOnly);
        /// <summary>
        /// Be sure to cache me, I am EXPENSIVE.
        /// </summary>
        /// <remarks>
        /// Cached reflection scan, but reflection Getters/Setters.
        /// </remarks>
        internal static PrefabDesc ClonePrefab(PrefabDesc from) {
            var ret = new PrefabDesc();
            foreach(var fi in _cloner.Value) {
                fi.SetValue(ret, fi.GetValue(from));
            }
            return ret;
        }
    }
}
