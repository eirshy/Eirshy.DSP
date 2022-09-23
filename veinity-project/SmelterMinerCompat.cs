using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using Eirshy.DSP.VeinityProject.Helpers;

//we are kind of a misnomer... we really just bulldoze Jinx's and sniff the wreckage to fake having it work right. Ish.

namespace Eirshy.DSP.VeinityProject {
    internal static class SmelterMinerCompat {
        const int MapABase = 9446;
        const int MapBBase = 9447;
        const int MapCBase = 9448;
        const int SplitOffset = 20;

        const int MapOBuilding = 9469;


        public static void SetUpAwake() {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(VeinityProject.GUID_SmelterMiner, out var bpi);
            if(bpi is null) return;
            var smt = bpi.Instance.GetType();
            const BindingFlags flags = BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
            var mc_iu = typeof(MinerComponent).GetMethod(nameof(MinerComponent.InternalUpdate));
            var mc_uvcp = typeof(StationComponent).GetMethod(nameof(StationComponent.UpdateVeinCollection));
            if(mc_iu is not null) VeinityProject.Harmony.Unpatch(mc_iu, smt.GetMethod("InternalUpdatePatch", flags));
            if(mc_uvcp is not null) VeinityProject.Harmony.Unpatch(mc_uvcp, smt.GetMethod("UpdateVeinCollectionPatch", flags));
        }
        public static void SetUpLate() {
            //_ = VeinityProject.Harmony.Patch(smt.GetMethod("UpdateVeinCollectionPatch", flags), new HarmonyMethod(disable));

            var oreMap = new Dictionary<EVeinType, int>(){
                { EVeinType.Iron, 1001 },
                { EVeinType.Copper, 1002 },
                { EVeinType.Silicium, 1003 },
                { EVeinType.Titanium, 1004 },
                { EVeinType.Stone, 1005 },
                { EVeinType.Coal, 1006 },
                { EVeinType.Oil, 1007 },
                { EVeinType.Fireice, 1011 },
                { EVeinType.Diamond, 1012 },
                { EVeinType.Fractal, 1013 },
                { EVeinType.Crysrub, 1117 },
                { EVeinType.Grat, 1014 },
                { EVeinType.Bamboo, 1015 },
                { EVeinType.Mag, 1016 },
            };
            const int NMap = 0;
            var prodMap = new Dictionary<EVeinType, int[]>(){
                { EVeinType.Iron,       new[]{ 1101, 1102, NMap } },
                { EVeinType.Copper,     new[]{ 1104, 1104, NMap } },
                { EVeinType.Silicium,   new[]{ 1105, 1105, NMap } },
                { EVeinType.Titanium,   new[]{ 1106, 1106, NMap } },
                { EVeinType.Stone,      new[]{ 1108, 1110, NMap } },
                { EVeinType.Coal,       new[]{ 1109, 1109, NMap } },
                { EVeinType.Oil,        new[]{ NMap, NMap, 1114 } },
                { EVeinType.Fireice,    new[]{ NMap, NMap, 1123 } },
                { EVeinType.Diamond,    new[]{ 1112, 1112, NMap } },
                { EVeinType.Fractal,    new[]{ NMap, NMap, 1113 } },
                { EVeinType.Crysrub,    new[]{ NMap, NMap, NMap } },
                { EVeinType.Grat,       new[]{ NMap, NMap, NMap } },
                { EVeinType.Bamboo,     new[]{ NMap, NMap, 1124 } },
                { EVeinType.Mag,        new[]{ NMap, NMap, NMap } },
            };
            var baseIDMap = new[]{ MapABase, MapBBase, MapCBase };

            var vmops = prodMap.SelectMany(kvp =>
                kvp.Value.Select((prod, i) => (
                    vein: kvp.Key,
                    miner: baseIDMap[i],
                    ore: oreMap[kvp.Key],
                    prod
                ))
            ).Where(vmop => vmop.prod != NMap)
            ;
            bool didOil = false;

            foreach(var vmop in vmops) {
                //so glad this is not in a hot context....
                var rec = LDB.recipes.dataArray
                    .FirstOrDefault(rec => rec.Items.Contains(vmop.ore) && rec.Results.Contains(vmop.prod))
                ;
                if(rec == null) {
                    VeinityProject.Logs.LogWarning(
                        $"Missing recipe for vein {vmop.vein} => {vmop.prod}"
                    );
                    continue;
                }

                var qtyIn = rec.ItemCounts
                    .Where((_,i) => rec.Items[i] == vmop.ore)
                    .First()
                ;
                var qtyOut = rec.ResultCounts
                    .Where((_,i) => rec.Results[i] == vmop.prod)
                    .First()
                ;
                var per = OreRemap.CalcOre2Product(qtyIn, qtyOut);
                OreRemap.Register(vmop.miner, vmop.ore, per, vmop.prod);
                OreRemap.Register(vmop.miner + SplitOffset, vmop.ore, per, vmop.prod);
                if(vmop.vein == EVeinType.Oil && vmop.prod == 1114 && !didOil) {
                    OreRemap.Register(MapOBuilding, vmop.ore, per, vmop.prod);
                    didOil = true;
                }
            }
        }

        static IEnumerable<CodeInstruction> DisableIL_retTrue() {
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ret);
        }

    }
}
