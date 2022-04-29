using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

using Eirshy.DSP.Rythmn;
using Eirshy.DSP.Rythmn.Enums;
using Eirshy.DSP.Rythmn.Protos;

namespace Eirshy.DSP.StaticCompression.Verses {
    class RailsAndSilos : StaticVerse {
        public override EOnLoadPrefabSync WillNeedSyncsFor => 
            EOnLoadPrefabSync.FactoryEjector 
            | EOnLoadPrefabSync.FactorySilo
            | EOnLoadPrefabSync.PowerConsumer
        ;

        int SiloMult { get; set; }
        int RailMult { get; set; }
        protected override void _stanza_config(ConfigFile config) {
            const string HDR = nameof(RailsAndSilos);
            RailMult = config.Bind<int>(HDR, nameof(RailMult), 1, new ConfigDescription(
                "Multiplier for EM Railguns. Also multiplies power cost."
                , new AcceptableValueRange<int>(1, 12)
            )).Value;
            SiloMult = config.Bind<int>(HDR, nameof(SiloMult), 1, new ConfigDescription(
                "Multiplier for Rocket Silos. Also multiplies power cost."
                , new AcceptableValueRange<int>(1, 12)
            )).Value;
        }


        protected override void _stanza_setup_ProtosCreated() {
            created_doRails();
            created_doSilos();
        }

        //"Frame" values are in 60-per-sec if we ever wanna change this to be belt-aware.
        void created_doRails() {
            if(RailMult == 1) return;
            var rails = LDB.items.dataArray.Where(ip => ip.prefabDesc.isEjector).ToList();
            foreach(var rail in rails) {
                rail.prefabDesc.ejectorChargeFrame /= RailMult;
                rail.prefabDesc.ejectorColdFrame /= RailMult;
                rail.prefabDesc.workEnergyPerTick *= RailMult;
            }
        }
        void created_doSilos() {
            if(SiloMult == 1) return;
            var silos = LDB.items.dataArray.Where(ip => ip.prefabDesc.isSilo).ToList();
            foreach(var silo in silos) {
                silo.prefabDesc.siloChargeFrame /= SiloMult;
                silo.prefabDesc.siloColdFrame /= SiloMult;
                silo.prefabDesc.workEnergyPerTick *= SiloMult;
            }
        }
    }
}
