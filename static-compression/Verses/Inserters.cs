using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

using Eirshy.DSP.Rythmn;
using Eirshy.DSP.Rythmn.Enums;

namespace Eirshy.DSP.StaticCompression.Verses {
    class Inserters : StaticVerse {
        public override EOnLoadPrefabSync WillNeedSyncsFor => EOnLoadPrefabSync.FactoryInserter;

        int STT_Factor { get; set; }
        int POW_Factor { get; set; }

        protected override void _stanza_config(ConfigFile config) {
            const string SEC_HDR = nameof(Inserters) + " (aka Sorters)";
            STT_Factor = config.Bind(SEC_HDR, nameof(STT_Factor), 1 , new ConfigDescription(
                "Decreases inserter/sorter Single Trip Time by this factor." +
                "\nNote that there is a cap on trip speed equal to the belt speed. Any speed in excess of that will merely shorten longer trips." +
                "\nFor example, using vanilla tier 3 belt/ins, cap is 30/s, base is 6/s. A factor of 8 will give you:" +
                "\n-- 6 -> 48/s theoretical max" +
                "\n-- 6 -> 30/s at distance 1" +
                "\n-- 3 -> 24/s at distance 2" +
                "\n-- 2 -> 16/s at distance 3" +
                "\nSuggested Values are 2-5, 8, 10, and 15." +
                "\nValues above 15 are of questionable utility without mod-added belts/inserters or a desire to use t2 ins with t3 belts."
                , new AcceptableValueRange<int>(1, 30)
            )).Value;
            POW_Factor = config.Bind(SEC_HDR, nameof(POW_Factor), -1 , new ConfigDescription(
                "Multiplies the active power consumption of Sorters by this value." +
                "\n- If negative, instead just uses STT_Factor directly." +
                "\n- If 0, will additionally set the idle energy cost to zero."
                , new AcceptableValueRange<int>(-1, 30)
            )).Value;
        }
        protected override void _stanza_setup_ProtosCreated() {
            created_DoInserters();
        }

        void created_DoInserters() {
            if(STT_Factor <= 1 && POW_Factor < 0) return;

            var multPow = POW_Factor < 0 ? STT_Factor : POW_Factor;

            var inses = LDB.items.dataArray.Where(ip => ip.prefabDesc.isInserter).ToList();
            foreach(var ins in inses) {
                ins.prefabDesc.inserterSTT /= STT_Factor;
                ins.prefabDesc.workEnergyPerTick *= multPow;
                if(multPow == 0) ins.prefabDesc.idleEnergyPerTick = 0;
            }
        }
    }
}
