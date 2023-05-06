using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
                | (LazyOutposting.GiveTechDwarvesLongPicks ? DwarfMission.EGear.LongPicks : 0)
            ;
            if(gear == DwarfMission.EGear.StandardOnly) return;//Vanilla dwarves have standard gear already

            Mission = new DwarfMission(gear);
            LazyOutposting.Harmony.PatchAll(typeof(DwarvenContract));
        }


        readonly static ConcurrentDictionary<int, PrefabDesc> Forgeries = new ConcurrentDictionary<int, PrefabDesc>();
        static PrefabDesc GetForgery(BuildPreview pv) => Forgeries.GetOrAdd(pv.desc.modelIndex, ForgePrefabDesc);
        static PrefabDesc GetOriginal(BuildPreview pv) => LDB.models.Select(pv.desc.modelIndex).prefabDesc;

        static PrefabDesc ForgePrefabDesc(int id) {
            var ret = LazyOutposting.ClonePrefab(LDB.models.Select(id).prefabDesc);
            ret.veinMiner = false;
            return ret;
        }

        private struct DwarfMission {
            [Flags]
            internal enum EGear {
                StandardOnly = 0,
                Buckets = 0x1 << 0,
                Haulers = 0x1 << 1,
                LongPicks = 0x1 << 2,
            }

            public readonly EGear Equipment;
            public readonly bool HasBuckets;
            public readonly bool HasLongPicks;

            readonly EVeinType[] _hauling;
            public EVeinType CommuteVein => _hauling[LazyOutposting.OnKey % _hauling.Length];

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

        const int MIN_VC_PARR_SIZE = 2048;
        static readonly int[] VC_NULL_PARAMS = new int[MIN_VC_PARR_SIZE];


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
                    int[] commuteTargets = null;//don't calc immediately in case we're not needed.
                    if(commuteTargets == null) {
                        var veinTargets = new List<int>();
                        for(int i = veinPool.Length; i-->0;) {
                            if(veinPool[i].type != commuteVein) continue;
                            veinTargets.Add(veinPool[i].id);
                        }
                        commuteTargets = veinTargets.ToArray();
                    }
                    if(commuteTargets.Length > 0) {
                        //Array.Clear(____tmp_ids, 0, ____tmp_ids.Length);//unnecessary, we don't use it
                        PrebuildData prebuildData = default;
                        //following is not necessary, we give it our arr as the params arr.
                        //prebuildData.InitParametersArray(targets.Length);
                        prebuildData.parameters = commuteTargets;
                        prebuildData.paramCount = prebuildData.parameters.Length;
                        prebuildData.ArrageParametersArray();

                        if(pv.desc.isVeinCollector) {
                            if(pv.paramCount == 0) {//array redecl + padding currently present in source.
                                pv.parameters = new int[MIN_VC_PARR_SIZE];
                                pv.paramCount = MIN_VC_PARR_SIZE;
                            }
                            Array.Resize(ref pv.parameters, pv.paramCount + prebuildData.paramCount);
                            Array.Copy(prebuildData.parameters, 0, pv.parameters, pv.paramCount, prebuildData.paramCount);
                            pv.paramCount += prebuildData.paramCount;
                        } else {
                            pv.parameters = prebuildData.parameters;
                            pv.paramCount = prebuildData.paramCount;
                        }

                        SwapToForged(in __instance, in pv, ref __state);
                        continue;
                    }
                }
                #endregion
                #region Rest requires Tech Dwarves

                if(pv.desc.isVeinCollector) {
                    //temp-fake our params array to reduce alloc spam
                    if(pv.paramCount == 0) pv.parameters = VC_NULL_PARAMS;

                    Pose pose = new Pose(pv.lpos, pv.lrot);
                    Vector3 forward = pose.forward;
                    Vector3 center = pv.lpos.normalized * __instance.controller.cmd.test.magnitude + forward * -10.5f;
                    //no need to pre-clear _tmp_ids; not only does GVIANA do it, but it's also unnecessary in the first place
                    var localVeins = __instance.actionBuild.nearcdLogic.GetVeinsInAreaNonAlloc(center, 12f, ____tmp_ids);

                    Vector3 backward = -forward;
                    Vector3 right = pose.right;
                    EVeinType forType = EVeinType.None;
                    //we do this as sort of two passes to make the 'real' pass cheapest
                    for(int bti = localVeins; bti-- > 0;) {
                        var vpi = ____tmp_ids[bti];
                        if(vpi <= 0) continue;
                        ref var vein = ref veinPool[vpi];
                        if(vein.id != vpi) continue;
                        Vector3 offset = vein.pos - center;
                        if(offset.sqrMagnitude <= 100f
                            && Mathf.Abs(Vector3.Dot(offset, backward)) <= 7f
                            && Mathf.Abs(Vector3.Dot(offset, right)) <= 5.5f
                            && Mission.CanTarget(vein.type)
                        ) {
                            forType = vein.type;
                            break;
                        }
                    }

                    //If we have no vein match, we can auto-fail now
                    if(forType == EVeinType.None) {
                        pv.paramCount = MIN_VC_PARR_SIZE;//ensures our list is clear
                        ConditionFail(EBuildCondition.NeedResource, in __instance, ref __runOriginal, ref __result);
                        return;
                    } else if(pv.parameters == VC_NULL_PARAMS) {
                        //unfake our params array
                        pv.parameters = new int[MIN_VC_PARR_SIZE];
                        pv.paramCount = MIN_VC_PARR_SIZE;
                    }

                    bool useLongPicks = Mission.HasLongPicks;//force hoist to local
                    var bldTargets = new List<int>(localVeins);
                    for(int bti = localVeins; bti-- > 0;) {
                        var vpi = ____tmp_ids[bti];
                        if(vpi <= 0) continue;
                        ref var vein = ref veinPool[vpi];
                        if(vein.id != vpi) continue;
                        Vector3 offset = vein.pos - center;
                        if(forType == vein.type && (
                            useLongPicks || (
                                offset.sqrMagnitude <= 100f
                                && Mathf.Abs(Vector3.Dot(offset, backward)) <= 7f
                                && Mathf.Abs(Vector3.Dot(offset, right)) <= 5.5f
                            )
                        )) {
                            bldTargets.Add(vpi);
                        }
                    }

                    //Array.Clear(____tmp_ids, 0, ____tmp_ids.Length);//unnecessary, things that use it clear it
                    PrebuildData prebuildData = default;
                    //following is not necessary, we give it our arr as the params arr.
                    //prebuildData.InitParametersArray(targets.Length);
                    prebuildData.parameters = bldTargets.ToArray();
                    prebuildData.paramCount = prebuildData.parameters.Length;
                    prebuildData.ArrageParametersArray();

                    Array.Resize(ref pv.parameters, pv.paramCount + prebuildData.paramCount);
                    Array.Copy(prebuildData.parameters, 0, pv.parameters, pv.paramCount, prebuildData.paramCount);
                    pv.paramCount += prebuildData.paramCount;

                    SwapToForged(in __instance, in pv, ref __state);
                    continue;

                }
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
            if(__state == null) __state = new List<BuildPreview>(__instance.buildPreviews.Count);
            pv.desc = GetForgery(pv);
            __state.Add(pv);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        static void SwitchBackToRealPapers(ref List<BuildPreview> __state) {
            if(__state == null) return;//nothing to do
            foreach(var pv in __state) {
                pv.desc = GetOriginal(pv);
            }
        }
    }
}
