using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using HarmonyLib;

namespace Eirshy.DSP.LazyOutposting.Components {
    static class DwarvenCommute {

        static MissionRotation Mission;
        public static void SetUp() {
            if(LazyOutposting.SoftDepTricks.OceanMiners) {
                Mission = new MissionRotation(MissionRotation.EDwarvenKit.MineralOilWater);
            } else if(LazyOutposting.SoftDepTricks.OilMiners) {
                Mission = new MissionRotation(MissionRotation.EDwarvenKit.MineralOil);
            } else Mission = new MissionRotation(MissionRotation.EDwarvenKit.Mineral);
            LazyOutposting.Harmony.PatchAll(typeof(DwarvenCommute));
        }


        readonly static ConcurrentDictionary<int, PrefabDesc> Forgeries = new ConcurrentDictionary<int, PrefabDesc>();
        static PrefabDesc GetForgery(BuildPreview pv) => Forgeries.GetOrAdd(pv.desc.modelIndex, ForgePrefabDesc);
        static PrefabDesc GetOriginal(BuildPreview pv) => LDB.models.Select(pv.desc.modelIndex).prefabDesc;

        static PrefabDesc ForgePrefabDesc(int id) {
            var ret = LazyOutposting.ClonePrefab(LDB.models.Select(id).prefabDesc);
            ret.veinMiner = false;
            return ret;
        }

        private struct MissionRotation {
            public enum EDwarvenKit {
                Mineral, MineralOil, MineralOilWater
            }

            readonly EVeinType[] _Veins;
            public EVeinType CurrentVein => _Veins[LazyOutposting.OnKey % _Veins.Length];
            public bool UseVanilla => LazyOutposting.OnKey % _Veins.Length == 0;


            public MissionRotation(EDwarvenKit kit) {
                switch(kit) {
                    default: throw new NotImplementedException();
                    case EDwarvenKit.Mineral:
                        _Veins = new[] {
                            EVeinType.None,// slot 0 MUST be None! It's our UseVanilla setting!
                            EVeinType.Iron,
                            EVeinType.Copper,
                            EVeinType.Silicium,
                            EVeinType.Titanium,
                            EVeinType.Stone,
                            EVeinType.Coal,
			                //EVeinType.Oil, //reqs MiningComp IU rewrite to cap at
			                EVeinType.Fireice,
                            EVeinType.Diamond, // Kimberlite Ore
			                EVeinType.Fractal, // Fractal Silicon
			                EVeinType.Crysrub, // Organic Crystals
			                EVeinType.Grat, // Optical Grating Crystal
			                EVeinType.Bamboo, // Spiniform Stalagmite Crystal
			                EVeinType.Mag, // Unipolar Magnet
			                //EVeinType.Max,
                        };
                        return;
                    case EDwarvenKit.MineralOil:
                        _Veins = new[] {
                            EVeinType.None,// slot 0 MUST be None! It's our UseVanilla setting!
                            EVeinType.Iron,
                            EVeinType.Copper,
                            EVeinType.Silicium,
                            EVeinType.Titanium,
                            EVeinType.Stone,
                            EVeinType.Coal,
                            EVeinType.Oil, //reqs MiningComp IU rewrite to cap at
			                EVeinType.Fireice,
                            EVeinType.Diamond, // Kimberlite Ore
			                EVeinType.Fractal, // Fractal Silicon
			                EVeinType.Crysrub, // Organic Crystals
			                EVeinType.Grat, // Optical Grating Crystal
			                EVeinType.Bamboo, // Spiniform Stalagmite Crystal
			                EVeinType.Mag, // Unipolar Magnet
			                //EVeinType.Max,
                        };
                        return;
                    case EDwarvenKit.MineralOilWater:
                        _Veins = new[] {
                            EVeinType.None,// slot 0 MUST be None! It's our UseVanilla setting!
			                EVeinType.None, //Fake None, used for experimenting with collecting oceans
                            EVeinType.Iron,
                            EVeinType.Copper,
                            EVeinType.Silicium,
                            EVeinType.Titanium,
                            EVeinType.Stone,
                            EVeinType.Coal,
                            EVeinType.Oil, //reqs MiningComp IU rewrite to cap at
			                EVeinType.Fireice,
                            EVeinType.Diamond, // Kimberlite Ore
			                EVeinType.Fractal, // Fractal Silicon
			                EVeinType.Crysrub, // Organic Crystals
			                EVeinType.Grat, // Optical Grating Crystal
			                EVeinType.Bamboo, // Spiniform Stalagmite Crystal
			                EVeinType.Mag, // Unipolar Magnet
                        };
                        return;
                }
            }
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(BuildTool_Click), nameof(BuildTool_Click.CheckBuildConditions))]
        static void PresentForgedPapers(BuildTool_Click __instance, ref int[] ____tmp_ids, ref List<BuildPreview> __state) {
            //this is largely just a cleanup and refactor of PlanetwideMining by GoToNightmare
            //Note we *do* lose a collision box detection mask which I'm pretty sure is meant for vein collisions...
            //  but the whole point of this is that you can just bury and ignore all veins on the planet, so meh.

            if(Mission.UseVanilla) return;//not active, skip entirely
            EVeinType globalVein = Mission.CurrentVein;
            if(globalVein != EVeinType.None) {
                #region Real Veins work like this

                int[] targets = null;//don't calc immediately in case we're not needed.
                foreach(var pv in __instance.buildPreviews) {
                    if(!pv.desc.veinMiner) continue;//Dwarves only work the mines.
                    if(targets == null) {
                        VeinData[] veinPool = __instance.factory.veinPool;
                        var veinTargets = new List<int>();
                        for(int i = 0; i < veinPool.Length; i++) {
                            if(veinPool[i].type != globalVein) continue;
                            veinTargets.Add(veinPool[i].id);
                        }
                        targets = veinTargets.ToArray();
                    }
                    if(targets.Length == 0) return; //do default calcs, we have no resources to override with.

                    Array.Clear(____tmp_ids, 0, ____tmp_ids.Length);
                    PrebuildData prebuildData = default;
                    //following is not necessary, we give it our arr as the params arr.
                    //prebuildData.InitParametersArray(targets.Length);
                    prebuildData.parameters = targets;
                    prebuildData.paramCount = prebuildData.parameters.Length;
                    prebuildData.ArrageParametersArray();

                    if(pv.desc.isVeinCollector) {
                        if(pv.paramCount == 0) {//array padding currently present in source.
                            pv.parameters = new int[2048];
                            pv.paramCount = 2048;
                        }
                        Array.Resize(ref pv.parameters, pv.paramCount + prebuildData.paramCount);
                        Array.Copy(prebuildData.parameters, 0, pv.parameters, pv.paramCount, prebuildData.paramCount);
                        pv.paramCount += prebuildData.paramCount;
                    } else {
                        pv.parameters = prebuildData.parameters;
                        pv.paramCount = prebuildData.paramCount;
                    }

                    //forge the prefabDesc to claim we're not a vein miner, and let our postfix know to undo it.
                    if(__state == null) __state = new List<BuildPreview>(__instance.buildPreviews.Count);
                    pv.desc = GetForgery(pv);
                    __state.Add(pv);
                }

                #endregion
            } else {
                #region Fake veins need some major work
                var waterid = __instance.factory.planet.waterItemId;
                if(waterid <= 0) return;//not actually water
                int[] targets = new int[] { };//needs some way to hijack a VeinData...
                foreach(var pv in __instance.buildPreviews) {
                    if(!pv.desc.veinMiner) continue;//Dwarves only work the mines.
                    if(!pv.desc.isVeinCollector) continue;//Water-collecting dwarves only work with Collectors

                    Array.Clear(____tmp_ids, 0, ____tmp_ids.Length);
                    PrebuildData prebuildData = default;
                    //following is not necessary, we give it our arr as the params arr.
                    //prebuildData.InitParametersArray(targets.Length);
                    prebuildData.parameters = targets;
                    prebuildData.paramCount = prebuildData.parameters.Length;
                    prebuildData.ArrageParametersArray();

                    if(pv.paramCount == 0) {//array padding currently present in source.
                        pv.parameters = new int[2048];
                        pv.paramCount = 2048;
                    }
                    Array.Resize(ref pv.parameters, pv.paramCount + prebuildData.paramCount);
                    Array.Copy(prebuildData.parameters, 0, pv.parameters, pv.paramCount, prebuildData.paramCount);
                    pv.paramCount += prebuildData.paramCount;
                    
                    //forge the prefabDesc to claim we're not a vein miner, and let our postfix know to undo it.
                    if(__state == null) __state = new List<BuildPreview>(__instance.buildPreviews.Count);
                    pv.desc = GetForgery(pv);
                    __state.Add(pv);
                }
                #endregion
            }
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
