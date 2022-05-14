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
    public class LazyOutposting : BaseUnityPlugin {
        public const string MODID = "LazyOutposting";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "1.0.0.0";
        public const string NAME = "Lazy Outposting";

        internal const string OTHERMOD_BPTWEEKS = "org.kremnev8.plugin.BlueprintTweaks";

        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));

        static internal ManualLogSource Logs { get; private set; }

        internal static bool EnableDwarvenCommute { get; private set; }
        internal static bool EnableVaporCollection { get; private set; }

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage($"Lazy Outposting - For anywhere but Hoxxes IV!");

            //config
            const string hdr = nameof(LazyOutposting);
            EnableDwarvenCommute = Config.Bind<bool>(hdr, nameof(EnableDwarvenCommute), true, new ConfigDescription(
                "If we should enable planet-wide mining."
            )).Value;
            EnableVaporCollection = Config.Bind<bool>(hdr, nameof(EnableVaporCollection), true, new ConfigDescription(
                "If we should enable ocean collection regardless of said ocean being accessible."
            )).Value;

            if(EnableDwarvenCommute) Harmony.PatchAll(typeof(DwarvenCommute));
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
