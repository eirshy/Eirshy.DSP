using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

using Eirshy.DSP.VeinityProject.Enums;

namespace Eirshy.DSP.VeinityProject {
    internal static class Config {
        //water pumps don't have veins lol
        public const ESourceType OceanSourceType = ESourceType.Infinite;

        public static void Load(ConfigFile cf) {
            const string HDR = nameof(VeinityProject);
            const string HDR_SOURCE_MODES = HDR + ".SourceModes";
            const string HDR_SOURCE_CONFIG = HDR + ".SourceModes.Config";

            Buffer = cf.Bind<int>(HDR, nameof(Buffer), 50, new ConfigDescription(
                "Internal buffer size for Miners, Oil Pumps, and Water Pumps." +
                "\nValues below 1 will be treated as 1."
            )).Value;
            if(Buffer < 1) Buffer = 1;

            WaterPumpVeinCount = cf.Bind<int>(HDR, nameof(WaterPumpVeinCount), 1, new ConfigDescription(
                "Number of \"veins\" to pretend Water Pumps are harvesting from." +
                "\nValues below 1 will be treated as 1."
            )).Value;
            if(WaterPumpVeinCount < 1) WaterPumpVeinCount = 1;

            DisableDampers = cf.Bind<bool>(HDR, nameof(DisableDampers), false, new ConfigDescription(
                "If true, we'll ignore the 'output is getting kinda full' damper values."
            )).Value;

            var allSourceTypes = Enum.GetNames(typeof(ESourceType)).Where(s => s != $"{ESourceType._UNSET}").ToArray();
            _ = cf.Bind<bool>(HDR_SOURCE_MODES, "_OPTIONS", false, new ConfigDescription(
                "Acceptable values for options in this group are one of the following values:" +
                $"\n{string.Join(", ", allSourceTypes)}"
            ));
            if(Enum.TryParse<ESourceType>(cf.Bind<string>(HDR_SOURCE_MODES
                , nameof(VeinSourceType), $"{ESourceType.FiniteDepleting}", new ConfigDescription(
                    "The source depletion type for Ore Veins. Default is the same as Vanilla:" +
                    "\nHarvested at a rate scaled to vein count until fully depleted."
                    , new AcceptableValueList<string>(acceptableValues: allSourceTypes)
                )
            ).Value, out var veinSrc)) {
                VeinSourceType = veinSrc;
            } else VeinSourceType = ESourceType.FiniteDepleting;

            if(Enum.TryParse<ESourceType>(cf.Bind<string>(HDR_SOURCE_MODES,
                nameof(OilSourceType), $"{ESourceType.Diminishing}", new ConfigDescription(
                    "The source depletion type for Oil Seeps. Default is the same as Vanilla:" +
                    "\nHarvest at a rate scaled to vein richness, reducing it to a limit." +
                    $"\nSee the {nameof(DiminishLimit)} setting for more details." +
                    $" Use {ESourceType.InfiniteDiminished} if you want this rate, but without reduction."
                    , new AcceptableValueList<string>(acceptableValues: allSourceTypes)
                )
            ).Value, out var oilSrc)) {
                OilSourceType = oilSrc;
            } else OilSourceType = ESourceType.Diminishing;
            _ = cf.Bind<string>(HDR_SOURCE_MODES,
                nameof(OceanSourceType), $"{ESourceType.Infinite}"
                , new ConfigDescription(
                    "The source depletion type for Oceans. Default is the same as Vanilla:" +
                    "\nHarvest at a rate scaled to the source count (default: 1)." +
                    "\nThis value is currently completely ignored, as there is no tracked 'remaining ocean'."
                )
            );

            var finiteDepleteTargets = Enum.GetNames(typeof(EFiniteSourceConsumptionTarget));
            if(Enum.TryParse<EFiniteSourceConsumptionTarget>(cf.Bind<string>(HDR_SOURCE_CONFIG
                , nameof(FiniteSourceTargeting), $"{EFiniteSourceConsumptionTarget.Cyclic}"
                , new ConfigDescription(
                    $"How {ESourceType.FiniteDepleting} picks the vein to deplete." +
                    $" Default ({EFiniteSourceConsumptionTarget.Cyclic}) is the same as Vanilla:" +
                    $"\nIterate over the whole list of veins each tick, depleting whichver is currently selected." +
                    $"\nOther Options: \"{EFiniteSourceConsumptionTarget.Fullest}\" depletes the fullest first," +
                    $" \"{EFiniteSourceConsumptionTarget.Lowest}\" depletes the most empty first."
                    , new AcceptableValueList<string>(acceptableValues: finiteDepleteTargets)
                )
            ).Value, out var finiteDepleteTarget)) {
                FiniteSourceTargeting = finiteDepleteTarget;
            } else FiniteSourceTargeting = EFiniteSourceConsumptionTarget.Cyclic;

            DiminishLimit = cf.Bind<int>(HDR_SOURCE_CONFIG, nameof(DiminishLimit), 2500, new ConfigDescription(
                $"For any {ESourceType.Diminishing}-mode miners, controls how low a vein is allowed to get." +
                $"\n2500 is roughly 0.1/s, with 25000 being roughly 1/s. Keep in mind this is per vein." +
                "\nValues below 1 will be treated as 1."
            )).Value;
            if(DiminishLimit < 1) DiminishLimit = 1;
        }

        public static int Buffer { get; set; }
        public static int WaterPumpVeinCount { get; set; }
        public static bool DisableDampers { get; set; }

        public static ESourceType VeinSourceType { get; set; }
        public static ESourceType OilSourceType { get; set; }

        public static EFiniteSourceConsumptionTarget FiniteSourceTargeting { get; set; }
        public static int DiminishLimit { get; set; }

    }
}
