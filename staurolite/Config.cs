using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

namespace Eirshy.DSP.Staurolite {
    internal class Config {
        public static void Load(ConfigFile cf) {
            const string HDR = nameof(Staurolite);
            const string HDR_Spiling = HDR + ".Spiling";

            Spile = cf.Bind<bool>(HDR_Spiling, nameof(Spile), false, new ConfigDescription(
                "Whether splitters with boxes on top of them should pile their outputs when possible."
            )).Value;
            SpileAlways4 = cf.Bind<bool>(HDR_Spiling, nameof(SpileAlways4), false, new ConfigDescription(
                "If Spiling is enabled, whether we should always pile to 4 if possible." +
                "\nIf this option is disabled, we will use the Station Piling research level instead."
            )).Value;
        }

        public static bool Spile { get; set; }
        public static bool SpileAlways4 { get; set; }
    }
}
