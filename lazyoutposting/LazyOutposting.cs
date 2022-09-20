using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string VERSION = "1.2.0.0";
        public const string NAME = "Lazy Outposting";

        internal const string OTHERMOD_BPTWEEKS = "org.kremnev8.plugin.BlueprintTweaks";//unused atm
        internal const string OTHERMOD_VEINITYPROJECT = ROOT + "VeinityProject";

        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));

        static internal ManualLogSource Logs { get; private set; }

        internal static bool EnableVaporCollection { get; private set; } = true;

        internal static bool GiveDwarvesHaulers { get; private set; } = true;
        static bool _GiveDwarvesBuckets = true;
        internal static bool GiveDwarvesBuckets {
            get => _GiveDwarvesBuckets && VeinityProjectExists.Value;
            set => _GiveDwarvesBuckets = value;
        }
        internal static bool GiveTechDwarvesLongPicks { get; private set; } = false;

        internal static readonly Lazy<bool> VeinityProjectExists = new Lazy<bool>(
            ()=>BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(OTHERMOD_VEINITYPROJECT)
            , LazyThreadSafetyMode.PublicationOnly
        );

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage($"Lazy Outposting - For anywhere but Hoxxes IV!");

            //config
            const string HDR = nameof(LazyOutposting);
            const string HDR_DWARVES = HDR + "." + nameof(DwarvenContract);
            const string REQ_OTHER_MOD = "Requires the mod VeinityProject, otherwise will be ignored.";

            //migrate legacy setting names
            var configVer = Config.Bind<string>(HDR, "ConfigVersion", "0", new ConfigDescription(
                "Internal value used to migrate settings."
            ));
            var doSettingsMigration = configVer.Value != VERSION;
            if(doSettingsMigration) {
                var legacyDesc = new ConfigDescription("Legacy setting name. Should be removed.");
                var toDelete = new List<ConfigDefinition>();
                ConfigEntry<string> legacyValue;
                Dictionary<string,bool> readBool = new Dictionary<string, bool>(2){
                    { "true", true },
                    { "false", false },
                };
                int migrationLevel;
                switch(configVer.Value) {
                    default: migrationLevel = 0; break;
                    case "1.2.0.0": migrationLevel = 1; break;
                }

                if(migrationLevel < 1) {
                    const string key = "EnableDwarvenCommute";
                    legacyValue = Config.Bind<string>(HDR, key, "", legacyDesc);
                    if(readBool.TryGetValue(legacyValue.Value.ToLower(), out var val)) {
                        GiveDwarvesHaulers = val;
                    }
                    toDelete.Add(legacyValue.Definition);
                }
                if(migrationLevel < 1) {
                    const string key = "GiveDwarvesBuckets";
                    legacyValue = Config.Bind<string>(HDR, key, "", legacyDesc);
                    if(readBool.TryGetValue(legacyValue.Value.ToLower(), out var val)) {
                        GiveDwarvesBuckets = val;
                    }
                    toDelete.Add(legacyValue.Definition);
                }

                configVer.Value = VERSION;
                Logs.LogWarning($"Removed: ${toDelete.Select(Config.Remove).Where(b => b).Count()}/{toDelete.Count}");
            }

            //Non-Dwarf Settings
            EnableVaporCollection = Config.Bind<bool>(HDR, nameof(EnableVaporCollection), EnableVaporCollection, new ConfigDescription(
                "If we should enable ocean collection regardless of said ocean being accessible."
            )).Value;


            //Dwarf Settings
            GiveDwarvesHaulers = Config.Bind<bool>(HDR_DWARVES, nameof(GiveDwarvesHaulers), GiveDwarvesHaulers, new ConfigDescription(
                "If we should enable planet-wide mining. Usable by both regular miners and mk2 miners."
            )).Value;
            GiveDwarvesBuckets = Config.Bind<bool>(HDR_DWARVES, nameof(GiveDwarvesBuckets), GiveDwarvesBuckets, new ConfigDescription(
                $"{REQ_OTHER_MOD}" +
                $"\nIf we should allow miners to collect oil." +
                $" Note that only Tech Dwarves (Mk2 Miners (Miner Collectors)) can perform this action without Haulers."
            )).Value;
            GiveTechDwarvesLongPicks = Config.Bind<bool>(HDR_DWARVES, nameof(GiveTechDwarvesLongPicks), GiveTechDwarvesLongPicks, new ConfigDescription(
                "If we should change the way Mk2 Miners (Miner Collectors) pick veins to be a little less strict." +
                "\nIf set, still requires *a* vein to be directly under the machine's plate, but will grab any vein" +
                " of the same type within the scan radius of the center of the plate, rather than strictly under the plate."
            )).Value;


            if(doSettingsMigration) Config.Save();
            if(GiveDwarvesHaulers) DwarvenContract.SetUp();
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
