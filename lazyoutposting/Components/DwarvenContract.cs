using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

using HarmonyLib;

using UnityEngine;

namespace Eirshy.DSP.LazyOutposting.Components {
    static class DwarvenContract {

        static DwarfMission Mission;
        public static void SetUp() {
            var gear = DwarfMission.EGear.StandardOnly
                | (LazyOutposting.GiveDwarvesHaulers ? DwarfMission.EGear.Haulers : 0)
                | (LazyOutposting.GiveDwarvesBuckets ? DwarfMission.EGear.Buckets : 0)
                | (LazyOutposting.GiveDwarvesLongPicks ? DwarfMission.EGear.LongPicks : 0)
                | (LazyOutposting.GiveDwarvesShovels ? DwarfMission.EGear.Shovels : 0)
            ;
            if(gear == DwarfMission.EGear.StandardOnly) return;//Vanilla dwarves have standard gear already

            Mission = new DwarfMission(gear);
            LazyOutposting.Harmony.PatchAll(typeof(DwarvenContract));
        }

        static readonly Lazy<MethodInfo> MinerIsVeinInRange = new(()=>{
            return typeof(MinerComponent).GetMethod(nameof(MinerComponent.IsTargetVeinInRange), BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        }, LazyThreadSafetyMode.PublicationOnly);


        readonly static ConcurrentDictionary<int, PrefabDesc> ForgeriesNonMiner = new();

        static PrefabDesc GetForgeryNonMiner(PrefabDesc pd) => ForgeriesNonMiner.GetOrAdd(pd.modelIndex, ForgeAsNonMiner);
        static PrefabDesc ForgeAsNonMiner(int id) {
            var ret = LazyOutposting.ClonePrefab(LDB.models.Select(id).prefabDesc);
            ret.veinMiner = false;//hides from BuildTool_Click
            ret.minerType = EMinerType.None;//hides from PlanetFactory
            return ret;
        }

        static PrefabDesc GetOriginal(PrefabDesc pd) => LDB.models.Select(pd.modelIndex).prefabDesc;


        private readonly struct DwarfMission {
            [Flags]
            internal enum EGear {
                StandardOnly = 0,
                Buckets = 0x1 << 0,
                Haulers = 0x1 << 1,
                LongPicks = 0x1 << 2,
                Shovels = 0x1 << 3,
            }

            public readonly EGear Equipment;
            public readonly bool HasBuckets;
            public readonly bool HasLongPicks;
            public readonly bool HasShovels;

            readonly EVeinType[] _hauling;
            public EVeinType CommuteVein => _hauling[LazyOutposting.OnKey % _hauling.Length];
            public bool HaulersNotActive => CommuteVein == EVeinType.None;

            readonly bool[] _dwarfTargets;
            public bool CanTarget(EVeinType veinType) => _dwarfTargets[(int)veinType];

            public DwarfMission(EGear withGear) {
                Equipment = withGear;
                var allEVT = (EVeinType[])Enum.GetValues(typeof(EVeinType));
                var stdBan = new HashSet<EVeinType>(3){
                    EVeinType.None,
                    EVeinType.Max,
                    EVeinType.Oil,
                };
                var validTargets = allEVT
                    .Where(evt => !stdBan.Contains(evt))
                    .ToList()
                ;

                //simple tech
                #region handle Buckets

                HasBuckets = Equipment.HasFlag(EGear.Buckets);
                if(HasBuckets) validTargets.Add(EVeinType.Oil);

                #endregion
                #region handle Long Picks

                HasLongPicks = Equipment.HasFlag(EGear.LongPicks);

                #endregion
                #region handle Shovels

                HasShovels = HasLongPicks || Equipment.HasFlag(EGear.Shovels);

                #endregion

                //complicated tech
                #region handle Haulers (assumes ValidTargets is finished)

                if(Equipment.HasFlag(EGear.Haulers)) {
                    _hauling = validTargets.Prepend(EVeinType.None).ToArray();
                } else _hauling = new[] { EVeinType.None };

                #endregion
                //array-style dictionary
                _dwarfTargets = new bool[(int)allEVT.Max()];
                for(int vti = validTargets.Count; vti-- > 0;) {
                    _dwarfTargets[(int)validTargets[vti]] = true;
                }
            }
        }

        static int[] VC_NULL_PARAMS => Array.Empty<int>();


        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        static void PresentForgedPapers(BuildTool_Click __instance, ref int[] ____tmp_ids, ref List<BuildPreview> __state
            , ref bool __runOriginal, ref bool __result
        ) {
            if(!__runOriginal) return;//someone told us to not
            //if we exist, we have work; our setup skips telling Harmony to do us otherwise

            EVeinType commuteVein = Mission.CommuteVein;//make immutable
            VeinData[] veinPool = __instance.factory.veinPool;
            foreach(var pv in __instance.buildPreviews) {
                if(!pv.desc.veinMiner) continue;
                #region Haulers -- works with all dwarves!
                if(commuteVein != EVeinType.None) {
                    int[] commuteTargets = null;//theoretically we can cache this somehow
                    if(commuteTargets == null) {
                        var veinTargets = new List<int>();
                        for(int i = veinPool.Length; i-->0;) {
                            if(veinPool[i].type != commuteVein) continue;
                            veinTargets.Add(veinPool[i].id);
                        }
                        commuteTargets = veinTargets.ToArray();
                    }
                    if(commuteTargets.Length > 0) {
                        //Array.Clear(____tmp_ids, 0, ____tmp_ids.Length);//unnecessary, things that use it clear it

                        //we overwrite the consumer, PBData is now unnecessary entirely.

                        pv.parameters = commuteTargets;
                        pv.paramCount = commuteTargets.Length;

                        SwapToForged(in __instance, in pv, ref __state);
                        continue;
                    }
                }
                #endregion
                #region Rest (non-Hauler)

                //We could likely eek a bit more perf out of this part by *not* using MC.IsTargetVeinInRange
                // but it's only vector math that can hoist some computations. Most complex is:
                //  lpose.position + forward * centerOffset
                //The futureproofing of using MC.ITVIR is probably the smarter move, especially since
                // any mods adding new miners that need such a thing will likely use it. Only real limiting
                // factor that we're imposing is that the offsets for fuzzy-ing are fixed by us.
                //TODO, probably hoist those so they can be parameterized in some way.
                //-eir

                Pose lpose = new Pose(pv.lpos, pv.lrot);
                Vector3 forward = lpose.forward;
                float fuzzyDist;
                float centerOffset;
                if(pv.desc.isVeinCollector) {
                    centerOffset = -10f;//BuildTool_Click.CheckBuildConditions::945
                    fuzzyDist = 18;//BuildTool_Click.CheckBuildConditions::946
                } else {
                    centerOffset = -1.2f;//BuildTool_Click.CheckBuildConditions::978
                    fuzzyDist = 12;//BuildTool_Click.CheckBuildConditions::979
                }
                Vector3 centerFuzzy = pv.lpos.normalized * __instance.controller.cmd.test.magnitude + forward * centerOffset;

                //no need to pre-clear _tmp_ids; not only does GVIANA do it, but it's also unnecessary in the first place
                var localVeins = __instance.actionBuild.nearcdLogic.GetVeinsInAreaNonAlloc(centerFuzzy, fuzzyDist, ____tmp_ids);

                EVeinType forType = EVeinType.None;
                bool useLongPicks = Mission.HasLongPicks;//force hoist to local

                //first pass, determine resource type (pick first valid)
                for(int bti = localVeins; bti-- > 0;) {
                    var vpi = ____tmp_ids[bti];
                    if(vpi <= 0) continue;
                    ref var vein = ref veinPool[vpi];
                    if(vein.id != vpi) continue;
                    if(Mission.CanTarget(vein.type) && MinerComponent.IsTargetVeinInRange(vein.pos, lpose, pv.desc)) {
                        forType = vein.type;
                        break;
                    }
                }

                //If we have no vein match, we can insta-fail now
                if(forType == EVeinType.None) {
                    pv.parameters = VC_NULL_PARAMS;
                    pv.paramCount = 0;//ensures our list is clear
                    ConditionFail(EBuildCondition.NeedResource, in __instance, ref __runOriginal, ref __result);
                    return;
                }

                //second pass, cull to only valid veins for current equipment
                var bldTargets = new List<int>(localVeins);
                for(int bti = localVeins; bti-- > 0;) {
                    var vpi = ____tmp_ids[bti];
                    if(vpi <= 0) continue;
                    ref var vein = ref veinPool[vpi];
                    if(vein.id != vpi) continue;
                    if(forType == vein.type && (
                        useLongPicks || MinerComponent.IsTargetVeinInRange(vein.pos, lpose, pv.desc)
                    )) {
                        bldTargets.Add(vpi);
                    }
                }


                //Array.Clear(____tmp_ids, 0, ____tmp_ids.Length);//unnecessary, things that use it clear it
                //PBData is useless to us, so don't bother with it.

                //avoid excessive reallocs by initing to the local size; resize to ensure we'll fit
                if(pv.parameters == VC_NULL_PARAMS) pv.parameters = new int[localVeins];
                else Array.Resize(ref pv.parameters, bldTargets.Count);

                bldTargets.CopyTo(pv.parameters);
                pv.paramCount = bldTargets.Count;

                SwapToForged(in __instance, in pv, ref __state);
                continue;

                #endregion
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ConditionFail(EBuildCondition condition, in BuildTool_Click __instance, ref bool __runOriginal, ref bool __result) {
            __runOriginal = false;
            __result = false;
            __instance.actionBuild.model.cursorState = -1;
            __instance.actionBuild.model.cursorText =  BuildPreview.GetConditionText(condition);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SwapToForged(in BuildTool_Click __instance, in BuildPreview pv, ref List<BuildPreview> __state) {
            __state ??= new List<BuildPreview>(__instance.buildPreviews.Count);
            pv.desc = GetForgeryNonMiner(pv.desc);
            __state.Add(pv);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        static void SwitchBackToRealPapers(ref List<BuildPreview> __state) {
            if(__state == null) return;//nothing to do
            foreach(var pv in __state) {
                pv.desc = GetOriginal(pv.desc);
            }
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        static void ForgePlanetFactoryHandling(int entityId, ref PrefabDesc desc, int prebuildId, in PlanetFactory __instance, ref int __state) {
            if(desc.minerType == EMinerType.None || desc.minerPeriod <= 0) return;//desc isn't our type.

            if(prebuildId <= 0) return;//invalid prebuild
            ref var prebuild = ref __instance.prebuildPool[prebuildId];
            if(prebuild.id != prebuildId) return;//disposed prebuild

            var minerId = __instance.factorySystem.NewMinerComponent(entityId, desc);
            if(minerId == 0) return;//failed to generate miner component; switch to default execution so default recovery may work
            ref var miner = ref __instance.factorySystem.minerPool[minerId];
            ref var sign = ref __instance.entitySignPool[entityId];

            if(Mission.HasShovels || !Mission.HaulersNotActive) {
                //miner.InitVeinArray(prebuild.paramCount);//effectively unwrapped
                miner.veins = new int[prebuild.paramCount];
                Array.Copy(prebuild.parameters, 0, miner.veins, 0, prebuild.paramCount);
                miner.veinCount = prebuild.paramCount;
                //miner.ArrangeVeinArray();//we exact-sized it, no need
            } else {
                var kept = new List<int>(prebuild.paramCount);
                var lpose = new Pose(prebuild.pos, prebuild.rot);
                for(int i = prebuild.paramCount; i-- > 0;) {
                    var vpi = prebuild.parameters[i];
                    if(vpi <= 0) continue;
                    var vpos = __instance.veinPool[vpi].pos;
                    if(vpos.magnitude < __instance.planet.realRadius - 40f) {
                        vpos = vpos.normalized * __instance.planet.realRadius;
                    }
                    if(MinerComponent.IsTargetVeinInRange(vpos, lpose, desc)) {
                        kept.Add(vpi);
                    }
                }

                //miner.InitVeinArray(prebuild.paramCount);//effectively unwrapped
                miner.veins = kept.ToArray();
                miner.veinCount = miner.veins.Length;
                //miner.ArrangeVeinArray();//.ToArray is exact-sized, no need
            }

            for(int vpi = miner.veinCount; vpi-- > 0;) {
                __instance.RefreshVeinMiningDisplay(miner.veins[vpi], entityId, 0);
            }
            miner.GetMinimumVeinAmount(__instance, __instance.veinPool);

            if(miner.type == EMinerType.Water) {
                sign.iconId0 = (uint)__instance.planet.waterItemId;
                if(sign.iconId0 > 12000U) sign.iconId0 = 0U;
                sign.iconType = (sign.iconId0 > 0U) ? 1U : 0U;
            } else {
                int firstVeinId = (miner.veinCount == 0) ? 0 : miner.veins[0];
                sign.iconId0 = (uint)((firstVeinId == 0) ? prebuild.filterId : __instance.veinPool[firstVeinId].productId);
                sign.iconType = sign.iconId0 > 0U ? 1U : 0U;
            }

            if(miner.type == EMinerType.Vein && !__instance.gameData.gameDesc.isInfiniteResource) {
                miner.GetTotalVeinAmount(__instance.veinPool);
            }

            //We've handled the minerType section, disable us and register for pcID rehook
            desc = GetForgeryNonMiner(desc);
            __state = entityId;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.CreateEntityLogicComponents))]
        static void RehookMinerPowerConsumer(int entityId, ref PrefabDesc desc, int prebuildId, in PlanetFactory __instance, ref int __state) {
            if(__state <= 0) return;//we didn't trip, so skip.

            ref var entity = ref __instance.entityPool[entityId];
            ref var miner = ref __instance.factorySystem.minerPool[entity.minerId];
            miner.pcId = entity.powerConId;
        }



        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BuildingGizmo), nameof(BuildingGizmo.Update))]
        static IEnumerable<CodeInstruction> BuildingGizmoFix(IEnumerable<CodeInstruction> raw) {
            var cis = raw.ToList();//unfortunate
            for(int ii = 0; ii < cis.Count; ii++) {
                var ins = cis[ii];
                if(ins.opcode == OpCodes.Ldc_I4  && (int)ins.operand == 2048) {
                    //just disallow ldc.i4 2048; it's a magic const only used by our stupid thing right now
                    //If it becomes a problem, we can isolate it better:
                    // currently first two ldc.i4 2048 after ldfld bool PrefabDesc::isVeinCollector
                    cis[ii].Reop(OpCodes.Ldc_I4_0);
                }
            }
            return cis;
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControlGizmo), nameof(PlayerControlGizmo.OnOutlineDraw))]
        static IEnumerable<CodeInstruction> PlayerControlGizmosFix(IEnumerable<CodeInstruction> raw) {
            var cis = raw.ToList();//unfortunate
            for(int ii = 0; ii < cis.Count; ii++) {
                var ins = cis[ii];
                if(ins.opcode == OpCodes.Ldc_I4  && (int)ins.operand == 2048) {
                    //just disallow ldc.i4 2048; it's a magic const only used by our stupid thing right now
                    //If it becomes a problem, we can isolate it better:
                    // currently first two ldc.i4 2048 after ldfld bool PrefabDesc::isVeinCollector
                    cis[ii].Reop(OpCodes.Ldc_I4_0);
                }
                if(ins.opcode == OpCodes.Call && ins.operand is MethodInfo mi && mi == MinerIsVeinInRange.Value) {
                    var prev = cis[ii-1];
                    if(prev.opcode != OpCodes.Ldfld) continue;
                    /**
            IL_0575: ldloc.s 36
            IL_0577: ldloc.s 32
            IL_0579: ldloc.s 24
            IL_057b: ldfld class PrefabDesc ModelProto::prefabDesc
                    vvv WE ARE HERE vvv
            IL_0580: call bool MinerComponent::IsTargetVeinInRange(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, valuetype [UnityEngine.CoreModule]UnityEngine.Pose, class PrefabDesc)
            IL_0585: brfalse IL_0614 
                     */
                    cis[ii-4].Reop(OpCodes.Nop);
                    cis[ii-3].Reop(OpCodes.Nop);
                    cis[ii-2].Reop(OpCodes.Nop);
                    cis[ii-1].Reop(OpCodes.Nop);
                    cis[ii].Reop(OpCodes.Ldc_I4_1);
                }
            }

            return cis;
        }


    }
}
