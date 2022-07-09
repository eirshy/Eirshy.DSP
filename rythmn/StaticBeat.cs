using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using BepInEx.Configuration;
using HarmonyLib;

using Eirshy.DSP.Rythmn.Enums;
using Eirshy.DSP.Rythmn.Utilities;
using Eirshy.DSP.Rythmn.Logging;

namespace Eirshy.DSP.Rythmn {
    internal static class StaticBeat {
        static ILogProvider Logger => RythmnKit.LogProvider;
        static EStationStorageSyncKeywords SSSMode = EStationStorageSyncKeywords.Never;
        static int SSSMode_CurMaxThreshold = 0;
        static StationStorageMatchSet SSSMode_MatchRegistry = null;

        internal static void SetUp(ConfigFile cf) {
            RythmnKit.Harmony.PatchAll(typeof(StaticBeat));

            const string HDR = nameof(StaticBeat);
            const string HDR_STATIONSYNC = HDR + ".StationSync";

            bool forceAllSyncs = cf.Bind<bool>(HDR, nameof(forceAllSyncs), false, new ConfigDescription(
                "If set, we'll run all known syncs regardless of which ones were requested." +
                "\nEquivalent to calling" +
                $" {nameof(RythmnKit)}.{nameof(RequestPrefabSync)}({nameof(EOnLoadPrefabSync)}.{EOnLoadPrefabSync._ALL_AVAILABLE});"
            )).Value;

            var sssModeList = cf.Bind<string>(HDR_STATIONSYNC, nameof(SSSMode)
                , $"{EStationStorageSyncKeywords.CurOverLimit} {EStationStorageSyncKeywords.TypeCollector}"
                , new ConfigDescription(
                "Space-delimited list of mode names. Note that items will NOT be deleted, even when reducing the max." +
                "\nOptions list (brackets for clarity):" +
                $" [{EStationStorageSyncKeywords.Always}], [{EStationStorageSyncKeywords.Never}]\n" +
                $", [{EStationStorageSyncKeywords.CurOverLimit}] reduces the max if it's over newMax" +
                $", [{EStationStorageSyncKeywords.CurMaxThreshold}] enables {nameof(SSSMode_CurMaxThreshold)}" +
                $", [{EStationStorageSyncKeywords.MatchRegistry}] enables {nameof(SSSMode_MatchRegistry)}" +
                $", [{EStationStorageSyncKeywords.TypeCollector}] sets max to newMax if the station is a Collector" +
                $", [{EStationStorageSyncKeywords.TypeMiner}] sets max to newMax if the station is a Miner" +
                ""
            )).Value;

            SSSMode = RythmnKit.ConfigSplit(sssModeList)
                .Select(s => Enum.TryParse<EStationStorageSyncKeywords>(s, out var ret) ? ret : EStationStorageSyncKeywords._nomatch)
                .Aggregate((nxt, cur) => cur | nxt)
            ;
            if(SSSMode.HasFlag(EStationStorageSyncKeywords._nomatch)) {
                Logger.Log("StaticBeat -- Station Storage Sync Mode has unknown mode in list.");
            }

            SSSMode_CurMaxThreshold = cf.Bind<int>(HDR_STATIONSYNC, nameof(SSSMode_CurMaxThreshold), 20000, new ConfigDescription(
                "If enabled, any station with its max set to at least this value will automatically be 'promoted' to newMax."
            )).Value;

            var SSSMode_ItemSDXRaw = cf.Bind<string>(HDR_STATIONSYNC, nameof(SSSMode_MatchRegistry), "", new ConfigDescription(
                "If enabled, will read the given space-delimited list of {Remote}{Local}[ItemID], and will set max to newMax if matched" +
                "\nRemote and Local both expect to be single letters: 's' for supply, 'd' for demand, 'x' for storage/none, 'a' for any, capitalized to invert" +
                "\nex1 : sd1001 : Iron Ore with Remote Supply and Local Demand" +
                "\nex2 : dS1006 : Coal with Remote Demand but NOT Local Supply" +
                "\nex3 : sa sX1210 xs1210 : Any Remote Supply, but warpers must either have supply remote and non-storage local, or storage remote and supply local"
            )).Value;
            var sssReg = new StationStorageMatchSet(SSSMode_ItemSDXRaw);
            //turn off MatchRegistry (and let it be GC'd) if there's no matchers
            if(!sssReg.HasMatchers) {
                SSSMode &= ~EStationStorageSyncKeywords.MatchRegistry;
            } else SSSMode_MatchRegistry = sssReg;


            //apply non-persisted
            if(forceAllSyncs) RequestPrefabSync(EOnLoadPrefabSync._ALL_AVAILABLE);
        }

        #region Harmony Hooks -- GameSave.LoadCurrentGame
        
        static void _runSetup() {
            try {
                if(_setups.IsValueCreated) {
                    Logger.Log($"The Static Beat :: SETTING THE STAGE!");
                    _setups.Value//already ordered, we're a sorted list
                        .Select(kvp => kvp.Value)
                        .DoForEach(act => act?.Invoke())
                    ;
                    _setups.Value.Clear();//let GC possibly have its toys back
                }
                SetupDone = true;
            } catch(Exception ex) {
                Logger.LogFatal(ex);
                throw ex;
            } finally {
                Logger.FlushLogBuffer();
            }
        }


        [HarmonyPostfix]
        [HarmonyAfter(RythmnKit.OTHER_MODS_LDBTOOL)]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        static void VFSetTheStage() {
            if(!SetupDone) _runSetup();
        }

        [HarmonyPrefix]
        [HarmonyAfter(RythmnKit.OTHER_MODS_LDBTOOL)]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame), new[] { typeof(string) })]
        static void SetTheStage(string saveName) {
            //We want anybody else doing a pre-load (ie, LDBTool) to go off first.
            if(DSPGame.IsMenuDemo) return;
            if(!SetupDone) _runSetup();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame), new[] { typeof(string) })]
        static void DanceToThe(string saveName) {
            if(DSPGame.IsMenuDemo) return;
            var gdat = GameMain.data;
            try {
                if(_load_pre != null) {
                    Logger.Log($"The Static Beat :: PRE-PERFORMANCE JAMMIN'! {_load_pre.GetInvocationList().Length} Registered!");
                    _load_pre(gdat);
                }
                if(WillSync != EOnLoadPrefabSync._NONE || _load_vis != null) {
                    var reqs = ((EOnLoadPrefabSync[])Enum.GetValues(typeof(EOnLoadPrefabSync)))
                        .Where(_needSync)
                        .ToList();
                    ;
                    reqs.Select(_getSyncPre)
                        .Where(del => del != null)
                        .DoForEach(syncPre => syncPre(gdat));
                    ;
                    var visitor = reqs
                        .Select(_getSync)
                        .Aggregate((cur, del) => cur += del)
                        + _load_vis
                    ;
                    int userAt = _load_vis?.GetInvocationList().Length ?? 0;
                    int syncAt = visitor.GetInvocationList().Length - (userAt > 0 ? 1 : 0);
                    Logger.Log($"The Static Beat :: THE MAIN SHOW! {syncAt} Syncs + {userAt} Registered");
                    EntityRef.GetEntityRefs(gdat)
                        .ToList()//materialize so the parallelizer for-sure has all info needed
                        .AsParallel()
                        //next is probably not required, but iunno how much heuristic it'll apply to visitor,
                        //  and I don't want a "But I can't!" to happen. "But I can't!" means someone screwed up.
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .ForAll(visitor)
                    ;
                }
                if(_load_post != null) {
                    Logger.Log($"The Static Beat :: PLAY THE OUTTRO! {_load_post.GetInvocationList().Length} Registered!");
                    _load_post(gdat);
                }
            } catch(Exception ex) {
                Logger.LogFatal(ex);
                throw ex;
            } finally {
                Logger.Log("The Static Beat :: SEE YA NEXT LOAD!");
                Logger.FlushLogBuffer();
            }
        }

        #endregion
        #region Harmony Hooks -- GameSave.SaveCurrentGame

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame), new[] { typeof(string) })]
        static void NoiseComplaint(string saveName) {
            if(DSPGame.IsMenuDemo) return;
            var gdat = GameMain.data;
            try {
                if(_savePre_pre != null) {
                    Logger.Log($"The Static Beat :: A NOISE COMPLAINT?! {_savePre_pre.GetInvocationList().Length} Registered!");
                    _savePre_pre(gdat);
                }
                if(_savePre_vis != null) {
                    Logger.Log($"The Static Beat :: EVERYBODY BE COOL! {_savePre_vis.GetInvocationList().Length} Registered!");
                    EntityRef.GetEntityRefs(gdat)
                        .ToList()
                        .AsParallel()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .ForAll(_savePre_vis)
                    ;
                }
                if(_savePre_post != null) {
                    Logger.Log($"The Static Beat :: OPENING THE DOOR IN 3... {_savePre_post.GetInvocationList().Length} Registered!");
                    _savePre_post(gdat);
                }
            } catch(Exception ex) {
                Logger.LogFatal(ex);
                throw ex;
            } finally {
                Logger.Log("The Static Beat :: NO PARTY HERE, OFFICER!");
                Logger.FlushLogBuffer();
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.SaveCurrentGame), new[] { typeof(string) })]
        static void RestartTheParty(string saveName) {
            if(DSPGame.IsMenuDemo) return;
            var gdat = GameMain.data;
            try {
                if(_savePost_pre != null) {
                    Logger.Log($"The Static Beat :: SEE YA, OFFICER! {_savePost_pre.GetInvocationList().Length} Registered!");
                    _savePost_pre(gdat);
                }
                if(_savePost_vis != null) {
                    Logger.Log($"The Static Beat :: WE MISSING ANYBODY? {_savePost_vis.GetInvocationList().Length} Registered!");
                    EntityRef.GetEntityRefs(gdat)
                        .ToList()
                        .AsParallel()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .ForAll(_savePost_vis)
                    ;
                }
                if(_savePost_post != null) {
                    Logger.Log($"The Static Beat :: RESTARTING THE JAMS IN 3... {_savePost_post.GetInvocationList().Length} Registered!");
                    _savePost_post(gdat);
                }
            } catch(Exception ex) {
                Logger.LogFatal(ex);
                throw ex;
            } finally {
                Logger.Log("The Static Beat :: TOLD YOU IT'D BE FINE.");
                Logger.FlushLogBuffer();
            }
        }

        #endregion

        #region Setup provider

        static bool SetupDone = false;
        readonly static Lazy<SortedList<ESetupPhase, Action>> _setups = new Lazy<SortedList<ESetupPhase, Action>>(() =>
            new SortedList<ESetupPhase, Action>(
                ((ESetupPhase[])Enum.GetValues(typeof(ESetupPhase))).ToDictionary(k => k, k => (Action)null)
            )
        , System.Threading.LazyThreadSafetyMode.PublicationOnly);
        internal static void AddSetup(Action todo, ESetupPhase phase) {
            if(!SetupDone) _setups.Value[phase] += todo;
        }
        #endregion
        #region On-Load providers

        static Action<GameData> _load_pre = null;
        static Action<EntityRef> _load_vis = null;
        static Action<GameData> _load_post = null;
        internal static void AddLoad_PreVisitor(Action<GameData> todo) => _load_pre += todo;
        internal static void AddLoad_Visitor(Action<EntityRef> todo) => _load_vis += todo;
        internal static void AddLoad_PostVisitor(Action<GameData> todo) => _load_post += todo;

        #endregion
        #region SavePre providers

        static Action<GameData> _savePre_pre = null;
        static Action<EntityRef> _savePre_vis = null;
        static Action<GameData> _savePre_post = null;
        internal static void AddSaveClean_PreVisitor(Action<GameData> todo) => _savePre_pre += todo;
        internal static void AddSaveClean_Visitor(Action<EntityRef> todo) => _savePre_vis += todo;
        internal static void AddSaveClean_PostVisitor(Action<GameData> todo) => _savePre_post += todo;

        #endregion
        #region SavePost providers

        static Action<GameData> _savePost_pre = null;
        static Action<EntityRef> _savePost_vis = null;
        static Action<GameData> _savePost_post = null;
        internal static void AddSaveRestore_PreVisitor(Action<GameData> todo) => _savePost_pre += todo;
        internal static void AddSaveRestore_Visitor(Action<EntityRef> todo) => _savePost_vis += todo;
        internal static void AddSaveRestore_PostVisitor(Action<GameData> todo) => _savePost_post += todo;

        #endregion

        #region Auto-Sync providers & mapping

        internal static EOnLoadPrefabSync WillSync { get; private set; } = EOnLoadPrefabSync._NONE;
        internal static void RequestPrefabSync(EOnLoadPrefabSync syncs) => WillSync |= syncs;
        static bool _needSync(EOnLoadPrefabSync sync) => (WillSync & sync) == sync;

        static Action<GameData> _getSyncPre(EOnLoadPrefabSync sync) {
            switch(sync) {
                default: return null;
                case EOnLoadPrefabSync.TransportStation_Storage: return PreSyncStation_Storage;
            }
        }
        static Action<EntityRef> _getSync(EOnLoadPrefabSync sync) {
            switch(sync) {
                default: return null;
                case EOnLoadPrefabSync.CargoBelt: return SyncBelt;
                case EOnLoadPrefabSync.CargoMonitor: return null;// SyncMonitor;
                case EOnLoadPrefabSync.CargoPiler: return null;// SyncPiler;
                case EOnLoadPrefabSync.CargoSplitter: return null;// SyncSplitter;
                case EOnLoadPrefabSync.CargoSpraycoater: return SyncSpraycoater;

                case EOnLoadPrefabSync.FactoryAssembler: return SyncAssembler;
                case EOnLoadPrefabSync.FactoryEjector: return SyncEjector;
                case EOnLoadPrefabSync.FactoryFractionator: return SyncFractionator;
                case EOnLoadPrefabSync.FactoryInserter: return SyncInserter;
                case EOnLoadPrefabSync.FactoryLab: return null;// SyncLab;
                case EOnLoadPrefabSync.FactoryMiner: return SyncMiner;
                case EOnLoadPrefabSync.FactorySilo: return SyncSilo;

                case EOnLoadPrefabSync.PowerAccumulator: return SyncPowerAccumulator;
                case EOnLoadPrefabSync.PowerConsumer: return SyncPowerConsumer;
                case EOnLoadPrefabSync.PowerExchanger: return SyncPowerExchanger;
                case EOnLoadPrefabSync.PowerGenerator: return SyncPowerGenerator;
                case EOnLoadPrefabSync.PowerNode: return SyncPowerNode;

                case EOnLoadPrefabSync.TransportStation_Harvesting: return SyncStation_Harvesting;
                case EOnLoadPrefabSync.TransportStation_Storage: return SyncStation_Storage;
                case EOnLoadPrefabSync.TransportStation_Drones: return null;// SyncStation_Drones;

                case EOnLoadPrefabSync.Other_Recipies: return SyncRecipes;
            }
        }

        #endregion
        static void Sync_Template(EntityRef entr) {
            if(!entr.Has_PowerNodeComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_PowerNodeComponent();
        }
        #region Standard Prefab Sync: Factory System

        static void SyncAssembler(EntityRef entr) {
            if(!entr.Has_AssemblerComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_AssemblerComponent();
            comp.speed = desc.assemblerSpeed;
        }
        static void SyncEjector(EntityRef entr) {
            if(!entr.Has_EjectorComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_EjectorComponent();
            comp.pivotY = desc.ejectorPivotY;
            comp.muzzleY = desc.ejectorMuzzleY;
            //yes, magic consts.
            comp.chargeSpend = desc.ejectorChargeFrame * 10000;
            comp.coldSpend = desc.ejectorColdFrame * 10000;
            comp.bulletId = desc.ejectorBulletId;
        }
        static void SyncFractionator(EntityRef entr) {
            if(!entr.Has_FractionatorComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_FractionatorComponent();
            comp.fluidInputMax = desc.fracFluidInputMax;
            comp.productOutputMax = desc.fracProductOutputMax;
            comp.fluidOutputMax = desc.fracFluidOutputMax;
        }
        static void SyncInserter(EntityRef entr) {
            if(!entr.Has_InserterComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_InserterComponent();
            
            var curTime = comp.time / comp.stt;
            
            //Might be able to infer STT's modifier via these values, but not sure.
            //can also use entr.Factory.ReadObjectConn() to get even more info; own-slots are 0 and 1.
            //comp.insertOffset;
            //comp.insertTarget;
            //comp.pickTarget;
            //comp.pickOffset;
            //comp.stt = desc.stt;//dangerous, would need to know original since this determines distance

            comp.canStack = desc.inserterCanStack;
            comp.delay = desc.inserterDelay;
            comp.stackSize = desc.inserterStackSize;

        }
        static void SyncLab(EntityRef entr) {
            //literally does nothing atm, case returns null
            if(!entr.Has_LabComponent) return;
            //var proto = entr.GetItem();
            //var desc = proto.prefabDesc;
            //ref var comp = ref entr.GetLive_LabComponent();
        }
        static void SyncMiner(EntityRef entr) {
            if(!entr.Has_MinerComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_MinerComponent();
            comp.period = desc.minerPeriod;
            comp.type = desc.minerType;
        }
        static void SyncSilo(EntityRef entr) {
            if(!entr.Has_SiloComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_SiloComponent();
            //more magic consts
            comp.chargeSpend = desc.siloChargeFrame * 10000;
            comp.coldSpend = desc.siloColdFrame * 10000;
            comp.bulletId = desc.siloBulletId;
        }

        #endregion
        #region Standard Prefab Sync: Power System

        static void SyncPowerAccumulator(EntityRef entr) {
            if(!entr.Has_PowerAccumulatorComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_PowerAccumulatorComponent();
            comp.inputEnergyPerTick = desc.inputEnergyPerTick;
            comp.outputEnergyPerTick = desc.outputEnergyPerTick;
            comp.maxEnergy = desc.maxAcuEnergy;
            if(comp.curEnergy > comp.maxEnergy) comp.curEnergy = comp.maxEnergy;
        }
        static void SyncPowerConsumer(EntityRef entr) {
            if(!entr.Has_PowerConsumerComponent) return;
            if(entr.Has_StationComponent) return;//don't touch stations
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_PowerConsumerComponent();
            comp.workEnergyPerTick = desc.workEnergyPerTick;
            comp.idleEnergyPerTick = desc.idleEnergyPerTick;
        }
        static void SyncPowerExchanger(EntityRef entr) {
            if(!entr.Has_PowerExchangerComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_PowerExchangerComponent();
            comp.energyPerTick = desc.exchangeEnergyPerTick;
            comp.maxPoolEnergy = desc.maxExcEnergy;
            if(comp.currPoolEnergy > comp.maxPoolEnergy) comp.currPoolEnergy = comp.maxPoolEnergy;
            comp.emptyId = desc.emptyId;
            comp.fullId = desc.fullId;
        }
        static void SyncPowerGenerator(EntityRef entr) {
            if(!entr.Has_PowerGeneratorComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_PowerGeneratorComponent();
            comp.genEnergyPerTick = desc.genEnergyPerTick;
            comp.useFuelPerTick = desc.useFuelPerTick;
            comp.photovoltaic = desc.photovoltaic;
            comp.wind = desc.windForcedPower;
            comp.gamma = desc.gammaRayReceiver;
            comp.geothermal = desc.geothermal;
            comp.genEnergyPerTick = desc.genEnergyPerTick;
            comp.useFuelPerTick = desc.useFuelPerTick;
            comp.fuelMask = (short)desc.fuelMask;
            comp.catalystId = desc.powerCatalystId;
            comp.productHeat = desc.powerProductHeat;
        }
        static void SyncPowerNode(EntityRef entr) {
            if(!entr.Has_PowerNodeComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_PowerNodeComponent();
            comp.connectDistance = desc.powerConnectDistance;
            comp.coverRadius = desc.powerCoverRadius;
            comp.isCharger = desc.isPowerCharger;
            //These casts are not something I'm overly confident in lol
            comp.workEnergyPerTick = unchecked((int)(desc.workEnergyPerTick));
            comp.idleEnergyPerTick = unchecked((int)(desc.idleEnergyPerTick));
        }

        #endregion
        #region Standard Prefab Sync: Cargo Traffic

        static void SyncBelt(EntityRef entr) {
            if(!entr.Has_BeltComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_BeltComponent();
            comp.speed = desc.beltSpeed;
        }
        static void SyncSplitter(EntityRef entr) {
            return;//literally does nothing atm, case returns null
            //if(!entr.Has_SplitterComponent) return;
            //var proto = entr.GetItem();
            //var desc = proto.prefabDesc;
            //ref var comp = ref entr.GetLive_SplitterComponent();
        }
        static void SyncMonitor(EntityRef entr) {
            return;//literally does nothing atm, case returns null
            //if(!entr.Has_MonitorComponent) return;
            //var proto = entr.GetItem();
            //var desc = proto.prefabDesc;
            //ref var comp = ref entr.GetLive_MonitorComponent();
        }
        static void SyncSpraycoater(EntityRef entr) {
            if(!entr.Has_SpraycoaterComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_SpraycoaterComponent();
            comp.incCapacity = desc.incCapacity;
        }
        static void SyncPiler(EntityRef entr) {
            return;//literally does nothing atm, case returns null
            //if(!entr.Has_PilerComponent) return;
            //var proto = entr.GetItem();
            //var desc = proto.prefabDesc;
            //ref var comp = ref entr.GetLive_PilerComponent();
        }

        #endregion
        #region Standard Prefab Sync: PlanetTransport

        //These are actively *hard*, so generally should leave them up to the modder.

        static void SyncStation_Harvesting(EntityRef entr) {
            if(!entr.Has_StationComponent) return;
            var proto = entr.GetItem();
            var _desc = proto.prefabDesc;
            if(!_desc.isCollectStation) return;
            var planetData = entr.Factory.planet;
            ref var comp = ref entr.GetLive_StationComponent();
            //about as good as I can do lol
            float mult = 0.016666668f * _desc.stationCollectSpeed;
            if(_desc.stationCollectSpeed * planetData.gasTotalHeat != 0.0) {
                mult *= (float)(1.0 - _desc.workEnergyPerTick / (_desc.stationCollectSpeed * planetData.gasTotalHeat * 0.016666666666666666));
            }
            for(int i = 0; i < comp.collectionPerTick.Length; i++) {
                comp.collectionPerTick[i] = planetData.gasSpeeds[i] * mult;
            }
        }
        
        static int SyncStation_Storage_localReseachBonus = 0;
        static int SyncStation_Storage_remoteReseachBonus = 0;
        static void PreSyncStation_Storage(GameData gd) {
            SyncStation_Storage_localReseachBonus = gd.history.localStationExtraStorage;
            SyncStation_Storage_remoteReseachBonus = gd.history.remoteStationExtraStorage;
        }
        static void SyncStation_Storage(EntityRef entr) {
            if(!entr.Has_StationComponent) return;
            var proto = entr.GetItem();
            var desc = proto.prefabDesc;
            ref var comp = ref entr.GetLive_StationComponent();
            //sync item counts
            if(comp.storage.Length != desc.stationMaxItemKinds) {
                if(comp.storage.Length < desc.stationMaxItemKinds) {
                    Array.Resize(ref comp.storage, desc.stationMaxItemKinds);//ez
                }else {//hard
                    //drones also track indexen, so to actually re-arrange the entries
                    //we'd have to scan for active drones and update them as well
                    //That's Extra Hard, so instead just cull from the end if possible lol
                    int keep = comp.storage.Length;
                    while(keep > 0 && comp.storage[keep - 1].itemId == 0) keep--;
                    if(keep < desc.stationMaxItemKinds) keep = desc.stationMaxItemKinds;
                    Array.Resize(ref comp.storage, keep);
                    //this is (basically) how the "clear item" action resets slots
                    for(int i = 0; i<comp.slots.Length; i++) {
                        if(comp.slots[i].storageIdx > keep) {
                            comp.slots[i].storageIdx = 0;
                            comp.slots[i].counter = 0;
                            comp.slots[i].dir = IODir.Output;
                        }
                    }
                }
            }

            //sync item maxes
            if(SSSMode == EStationStorageSyncKeywords.Never) return;
            var newMax = desc.stationMaxItemCount;
            if(desc.isStellarStation) newMax += SyncStation_Storage_remoteReseachBonus;
            else newMax += SyncStation_Storage_localReseachBonus;
            var allMax = SSSMode.HasFlag(EStationStorageSyncKeywords.Always)
                || desc.isCollectStation && SSSMode.HasFlag(EStationStorageSyncKeywords.TypeCollector)
                || desc.isVeinCollector && SSSMode.HasFlag(EStationStorageSyncKeywords.TypeMiner)
            ;

            for(int i=0; i< comp.storage.Length; i++) {
                //apply maxes. We don't have to worry about max being lower than count, game allows it.
                if(false
                    || (allMax && comp.storage[i].max != newMax)
                    || (SSSMode.HasFlag(EStationStorageSyncKeywords.CurOverLimit) && comp.storage[i].max > newMax)
                    || (SSSMode.HasFlag(EStationStorageSyncKeywords.CurMaxThreshold) && comp.storage[i].max >= SSSMode_CurMaxThreshold)
                    || (SSSMode.HasFlag(EStationStorageSyncKeywords.MatchRegistry) && SSSMode_MatchRegistry.IsMatch(comp.storage[i]))
                ) {
                    comp.storage[i].max = newMax;
                    continue;
                }
            }
        }

        #endregion
        #region Standard Prefab Sync: Other

        static void SyncRecipes(EntityRef entr) {
            if(entr.Has_AssemblerComponent) {
                #region AssemblerComponent

                ref var comp = ref entr.GetLive_AssemblerComponent();
                if(comp.recipeType != ERecipeType.None) {
                    var recipeProto = LDB.recipes.Select(comp.recipeId);
                    comp.recipeType = recipeProto.Type;
                    comp.timeSpend = recipeProto.TimeSpend * 10000;
                    comp.extraTimeSpend = recipeProto.TimeSpend * 100000;
                    comp.productive = recipeProto.productive;
                    comp.requires = recipeProto.Items;
                    comp.requireCounts = recipeProto.ItemCounts;
                    Array.Resize(ref comp.served, recipeProto.Items.Length);
                    Array.Resize(ref comp.incServed, recipeProto.Items.Length);
                    comp.products = recipeProto.Results;
                    comp.productCounts = recipeProto.ResultCounts;
                    Array.Resize(ref comp.produced, recipeProto.Results.Length);
                }

                #endregion
            }
            if(entr.Has_LabComponent) {//transcludes the duck code from AssemblerComponent
                #region LabComponent

                ref var comp = ref entr.GetLive_LabComponent();
                if(comp.matrixMode) {
                    var recipeProto = LDB.recipes.Select(comp.recipeId);
                    //comp.recipeType = recipeProto.Type;
                    comp.timeSpend = recipeProto.TimeSpend * 10000;
                    comp.extraTimeSpend = recipeProto.TimeSpend * 100000;
                    comp.productive = recipeProto.productive;
                    comp.requires = recipeProto.Items;
                    comp.requireCounts = recipeProto.ItemCounts;
                    Array.Resize(ref comp.served, recipeProto.Items.Length);
                    Array.Resize(ref comp.incServed, recipeProto.Items.Length);
                    comp.products = recipeProto.Results;
                    comp.productCounts = recipeProto.ResultCounts;
                    Array.Resize(ref comp.produced, recipeProto.Results.Length);
                }//nothing to do on other modes

                #endregion
            }
            if(entr.Has_FractionatorComponent) {//scans for recipe by in then out, logs if not found
                #region FractionatorComponent

                ref var comp = ref entr.GetLive_FractionatorComponent();
                var curIn = comp.fluidId;
                var curOut = comp.productId;
                var rec = RecipeProto.fractionatorRecipes.FirstOrDefault(rp => rp.Items.Length > 0 && rp.Items[0] == curIn);
                if(rec is null) {//try matching by output
                    rec = RecipeProto.fractionatorRecipes.FirstOrDefault(rp => rp.Results.Length > 0 && rp.Results[0] == curOut);
                }
                if(rec is null) {
                    Logger.Log($"Found Fractionator with unknown recipe! " +
                        $"Input: {LDB.ItemName(curIn)} Output: {LDB.ItemName(curOut)} at {comp.produceProb * 100f}%."
                    );
                }
                //if we still can't find it, give up lol
                if(rec != null) {
                    comp.fluidId = rec.Items[0];
                    comp.productId = rec.Results[0];
                    comp.produceProb = (float)rec.ResultCounts[0] / (float)rec.ItemCounts[0];
                }

                #endregion
            }
        }

        #endregion

    }
}
