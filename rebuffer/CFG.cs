using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BepInEx.Configuration;

namespace Eirshy.DSP.ReBuffer {
    static class CFG {
        public static void Load(ConfigFile cf) {
            const string HDR = nameof(ReBuffer);
            const string HDR_IN = HDR + ".RecipeInputs";
            const string HDR_OUT = HDR + ".RecipeOutputs";
            const string HDR_RES = HDR + ".Research";
            const string HDR_RYTHMN = HDR + ".Rythmn.Addons";
            const string REQ_RYTHMN = "Requires RythmnKit to also be installed.";

            var allTypes = ((ERecipeType[])Enum.GetValues(typeof(ERecipeType)));
            var cfgTypes =allTypes
                .Where(type => type != ERecipeType.None 
                    && type != ERecipeType.Fractionate//todo
                    && type != ERecipeType.PhotonStore//todo
                )
                .ToList()
            ;
            lookup_inp = new int[allTypes.Select(rt => (int)rt).Max()+1];
            lookup_oup = new int[lookup_inp.Length];
            for(int i= 0; i < lookup_inp.Length; i++) {
                var hasCast = Enum.IsDefined(typeof(ERecipeType), i);
                if(!hasCast) {
                    lookup_inp[i] = -1;
                    lookup_oup[i] = -1;
                } else {
                    var cast = (ERecipeType)i;
                    lookup_inp[i] = 3;
                    lookup_oup[i] = 5;
                }
            }

            foreach(var type in cfgTypes) {
                lookup_inp[(int)type] = cf.Bind<int>(HDR_IN, $"In{type}", 3, new ConfigDescription(
                    $"The multiplier for the input buffers for {type} ({(int)type}) recipes."
                    , new AcceptableValueRange<int>(1, 100)
                )).Value;
                lookup_oup[(int)type] = cf.Bind<int>(HDR_OUT, $"Out{type}", 5, new ConfigDescription(
                    $"The multiplier for the output buffers for {type} ({(int)type}) recipes."
                    , new AcceptableValueRange<int>(1, 100)
                )).Value;
            }

            JelloAppetite = cf.Bind<int>(HDR_RES, nameof(JelloAppetite), 10, new ConfigDescription(
                "The multiplier for the input buffers for hash-producing (research-mode) labs."
                , new AcceptableValueRange<int>(1, 100)
            )).Value;

            CollapseLabTowers = cf.Bind<bool>(HDR_RYTHMN, nameof(CollapseLabTowers), false, new ConfigDescription(
                $"{REQ_RYTHMN}" +
                $"\nIf true, we'll collapse all of your Lab Towers into just the bottom floor, allowing" +
                " all other labs to basically act as a flashy, overly tall hat." +
                "\nThis doesn't entirely remove them from the entity count, but it does make their tick" +
                " operations notably cheaper."
            )).Value;

        }

        internal static int[] lookup_inp;
        internal static int[] lookup_oup;
        internal static int JelloAppetite { get; private set; }
        internal static bool CollapseLabTowers { get; private set; }

        internal static int GetInp(ERecipeType @for) => lookup_inp[(int)@for];
        internal static int GetOup(ERecipeType @for) => lookup_oup[(int)@for];

    }
}
