using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

//we are kind of a misnomer... we really just bulldoze Jinx's and sniff the wreckage to fake having it work right. Ish.

namespace Eirshy.DSP.VeinityProject {
    internal static class SmelterMinerCompat {
        const int MapABase = 9446;
        const int MapBBase = 9447;
        const int MapCBase = 9448;
        const int MapOBase = 9449;
        const int SplitOffset = 20;

        
        public static void SetUp() {
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(VeinityProject.GUID_SmelterMiner, out var bpi);
            if(bpi is null) return;
            var smt = bpi.Instance.GetType();
            const BindingFlags flags = BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
            var mc_iu = typeof(MinerComponent).GetMethod(nameof(MinerComponent.InternalUpdate));
            var mc_uvcp = typeof(StationComponent).GetMethod(nameof(StationComponent.UpdateVeinCollection));
            if(mc_iu is not null) VeinityProject.Harmony.Unpatch(mc_iu, smt.GetMethod("InternalUpdatePatch", flags));
            if(mc_uvcp is not null) VeinityProject.Harmony.Unpatch(mc_uvcp, smt.GetMethod("UpdateVeinCollectionPatch", flags));
            
            //_ = VeinityProject.Harmony.Patch(smt.GetMethod("UpdateVeinCollectionPatch", flags), new HarmonyMethod(disable));

            //todo: make this reflection bs instead of manual
            var bases = new (int proto, int ore, int smelt, int per)[]{
                (MapABase, 1001, 1101, 1),
                (MapBBase, 1001, 1102, 1),

                (MapABase, 1002, 1104, 1),
                (MapBBase, 1002, 1104, 1),

                (MapABase, 1003, 1105, 2),
                (MapBBase, 1003, 1105, 2),

                (MapABase, 1004, 1106, 2),
                (MapBBase, 1004, 1106, 2),

                (MapABase, 1005, 1108, 1),
                (MapBBase, 1005, 1110, 2),

                (MapABase, 1006, 1109, 2),
                (MapBBase, 1006, 1109, 2),

                (MapABase, 1012, 1112, 1),
                (MapBBase, 1012, 1112, 1),

                (MapABase, 1013, 1113, 1),
                (MapBBase, 1013, 1113, 1),
                
                //--

                (MapCBase, 1011, 1123, 1),

                (MapCBase, 1015, 1124, 1),

                //--

                (MapOBase, 1007, 1114, 1),
            };

            var withAdv = bases.Concat(bases.Select(b => (proto: b.proto + SplitOffset, b.ore, b.smelt, b.per)));
            
            foreach(var maps in withAdv) {
                VeinityPatcher.AddRemapFor(maps.proto, maps.ore, maps.smelt, maps.per);
            }
            VeinityProject.Logs.LogInfo($"Remap Count: {VeinityPatcher.Remap.Count}");
        }

        static IEnumerable<CodeInstruction> Disable() {
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ret);
        }

    }
}
