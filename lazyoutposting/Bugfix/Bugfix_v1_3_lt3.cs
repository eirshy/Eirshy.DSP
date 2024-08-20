using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using HarmonyLib;

namespace Eirshy.DSP.LazyOutposting.Bugfix {
    internal static class Bugfix_v1_3_lt3 {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame), [typeof(string)])]
        static void FixDisconnectedMinerStations() {
            if(DSPGame.IsMenuDemo) return;
            LazyOutposting.Logs.LogWarning($"Looking for Disconnected Vein Collectors...");
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
            LazyOutposting.Logs.LogWarning($"...Found and re-linked {found} Vein Collectors across {potential.Count} factories!");
        }
    }
}
