using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

using UnityEngine;
using HarmonyLib;

namespace Eirshy.DSP.LazyOutposting.Components {
    static class VaporCollection {
        readonly static ConcurrentDictionary<int, PrefabDesc> Forgeries = new ConcurrentDictionary<int, PrefabDesc>();
        static PrefabDesc GetForgery(BuildPreview pv) => Forgeries.GetOrAdd(pv.desc.modelIndex, ForgePrefabDesc);
        static PrefabDesc GetOriginal(BuildPreview pv) => LDB.models.Select(pv.desc.modelIndex).prefabDesc;

        static PrefabDesc ForgePrefabDesc(int id) {
            var ret = LazyOutposting.ClonePrefab(LDB.models.Select(id).prefabDesc);
            ret.waterPoints = Array.Empty<Vector3>();
            return ret;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        static void PresentForgedPapers(BuildTool_Click __instance, ref List<BuildPreview> __state) {
            foreach(var pv in __instance.buildPreviews) {
                if(pv is null) continue;
                if(pv.desc.waterPoints.Length == 0) continue;//must require ocean
                if(pv.desc.geothermal) continue;//lava has additional calculations

                //Don't need anything special here, pumps are automatic so long as they can be placed.

                //forge the prefabDesc to claim we're not a vein miner, and let our postfix know to undo it.
                if(__state == null) __state = new List<BuildPreview>(__instance.buildPreviews.Count);
                pv.desc = GetForgery(pv);
                __state.Add(pv);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        static void PresentForgedPapers(BuildTool_BlueprintPaste __instance, ref List<BuildPreview> __state) {
            foreach(var pv in __instance.bpPool) {
                if(pv is null) continue;
                if(pv.desc.waterPoints.Length == 0) continue;//must require ocean
                if(pv.desc.geothermal) continue;//lava has additional calculations

                //Don't need anything special here, pumps are automatic so long as they can be placed.

                //forge the prefabDesc to claim we're not a vein miner, and let our postfix know to undo it.
                if(__state == null) __state = new List<BuildPreview>(__instance.buildPreviews.Count);
                pv.desc = GetForgery(pv);
                __state.Add(pv);
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        [HarmonyPatch(typeof(BuildTool_BlueprintPaste), nameof(BuildTool_BlueprintPaste.CheckBuildConditions))]
        static void SwiatchBackToRealPapers(ref List<BuildPreview> __state) {
            if(__state == null) return;//nothing to do
            foreach(var pv in __state) {
                pv.desc = GetOriginal(pv);
            }
        }

    }
}
