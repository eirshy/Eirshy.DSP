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
using System.Reflection.Emit;

namespace Eirshy.DSP.LazyOutposting {

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(OTHERMOD_VEINITYPROJECT, BepInDependency.DependencyFlags.SoftDependency)]
    public class LazyOutposting : BaseUnityPlugin {
        public const string MODID = "LazyOutposting";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string VERSION = "1.3.4";
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
        internal static bool GiveDwarvesShovels { get; private set; } = false;
        internal static bool GiveDwarvesLongPicks { get; private set; } = false;

        internal static readonly Lazy<bool> VeinityProjectExists = new Lazy<bool>(
            ()=>BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(OTHERMOD_VEINITYPROJECT)
            , LazyThreadSafetyMode.PublicationOnly
        );

        private void Awake() {
            Logs = Logger;
            Logger.LogMessage($"Lazy Outposting - For anywhere but Hoxxes IV!");

            //config -- todo: move this into its own file lol
            const string HDR = nameof(LazyOutposting);
            const string HDR_DWARVES = HDR + "." + nameof(DwarvenContract);
            const string HDR_BUGFIXES = HDR + ".zz.BugFixes";
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
                var readBool = new Dictionary<string, bool>(2){
                    { "true", true },
                    { "false", false },
                };
                int migrationLevel;
                switch(configVer.Value) {
                    default: migrationLevel = 0; break;
                    case "1.2.0.0": migrationLevel = 1; break;
                    case "1.2.1": migrationLevel = 1; break;
                    case "1.3.0": migrationLevel = 2; break;
                    case "1.3.1": migrationLevel = 2; break;
                    case "1.3.2": migrationLevel = 2; break;
                    case "1.3.3": migrationLevel = 2; break;
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

                if(migrationLevel < 2) {
                    const string key = "GiveTechDwarvesLongPicks";
                    legacyValue = Config.Bind<string>(HDR, key, "", legacyDesc);
                    if(readBool.TryGetValue(legacyValue.Value.ToLower(), out var val)) {
                        GiveDwarvesLongPicks = val;
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
            GiveDwarvesLongPicks = Config.Bind<bool>(HDR_DWARVES, nameof(GiveDwarvesLongPicks), GiveDwarvesLongPicks, new ConfigDescription(
                "If we should change the way mines pick veins to be \"fuzzy\"." +
                "\nIf set, still requires *a* vein to be in the machine's target zone, but will grab any vein of the same" +
                " type within the scanning zone. Additionally, enables Shovels, as testing height in this mode is difficult."
            )).Value;
            GiveDwarvesShovels = Config.Bind<bool>(HDR_DWARVES, nameof(GiveDwarvesShovels), GiveDwarvesShovels, new ConfigDescription(
                "If we should ignore vein height (ie, whether the vein's been burried)." +
                "\nNote that this is implied by Long Picks, due to a difficulty in implementing Long Picks without Shovels."
            )).Value;
            
            var anyDwarves = GiveDwarvesBuckets || GiveDwarvesHaulers || GiveDwarvesLongPicks || GiveDwarvesShovels;


            var run_Bugfix_v1_3_lt3 = Config.Bind<bool>(HDR_BUGFIXES, "Fix Issues: v1.3.0-v1.3.2", false, new ConfigDescription(
                "If we should run the on-game-load fixes for save game issues created by v1.3.0 through v1.3.2 of this mod." +
                "\nRequires us to iterate through all stations on all planets to reconnect parents. Note that deconstructing and" +
                " reconstructing an affected VeinCollector (speed slider is broken) will also fix this issue."
            )).Value;



            //------
            if(doSettingsMigration) Config.Save();
            //------
            if(anyDwarves) DwarvenContract.SetUp();
            if(EnableVaporCollection) VaporCollection.SetUp();
            if(run_Bugfix_v1_3_lt3) Harmony.PatchAll(typeof(Bugfix_v1_3_lt3));
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


        private static class Bugfix_v1_3_lt3 {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame), new[] { typeof(string) })]
            static void FixDisconnectedMinerStations() {
                if(DSPGame.IsMenuDemo) return;
                Logs.LogWarning($"Looking for Disconnected Vein Collectors...");
                var gdat = GameMain.data;
                var found = 0;
                var potential = gdat.factories
                    .Where(pf => pf != null && pf.transport != null && pf.transport.stationPool != null)
                    .ToList()
                ;
                //entity visitor, parallelize, but do so per-planet to keep it "simple"
                potential.AsParallel().ForAll(pf => {
                    foreach(var station in pf.transport.stationPool) {
                        if(false
                            || station == null
                            || !station.isVeinCollector
                            || station.entityId == 0 
                            || station.minerId != 0
                        ) continue;
                        station.minerId = pf.entityPool[station.entityId].minerId;
                        _ = Interlocked.Increment(ref found);
                    }
                });
                Logs.LogWarning($"...Found and re-linked {found} Vein Collectors across {potential.Count} factories!");
            }
        }

    }

    internal static class Extensions {
        /// <summary>
        /// Convenience; replaces the opcode and the operand on this instruction
        /// </summary>
        public static void Reop(this CodeInstruction ci, OpCode op, object operand = null) {
            ci.opcode = op;
            ci.operand = operand;
        }
    }
}
