using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

using Eirshy.DSP.VeinityProject.Enums;

namespace Eirshy.DSP.VeinityProject {
    internal static class Config {
        public static void Load(ConfigFile cf) {
            //todo
        }


        public static int Buffer = 64;//ez demonstration of working
        public static ESourceType VeinSourceType = ESourceType.FiniteDepleting;
        public static ESourceType OilSourceType = ESourceType.Diminishing;
        public const ESourceType WaterSourceType = ESourceType.Infinite;

        public static EFiniteSourceConsumptionTarget FiniteSourceTargeting = EFiniteSourceConsumptionTarget.Cyclic;
        /// <summary>
        /// 2500 per 0.1/s
        /// </summary>
        public static int DiminishLimit = 2500;
    }
}
