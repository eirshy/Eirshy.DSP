using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using BepInEx.Configuration;

namespace Eirshy.DSP.ReBuffer {
    static class Config {
        const string HDR = nameof(ReBuffer);
        const string HDR_BUGGY = HDR + ".Triage";
        const string HDR_IN = HDR + ".RecipeInputs";
        const string HDR_OUT = HDR + ".RecipeOutputs";
        const string HDR_RES = HDR + ".Research";
        const string HDR_RAYR = HDR + ".RayReciever";
        const string HDR_RYTHMN = HDR + ".Rythmn.Addons";

        const string REQ_ASM = "Requires Assemblers to be patched.";
        const string REQ_LAB = "Requires Labs to be patched.";
        const string REQ_RAYR = "Requires Power Generators to be patched (weirdly enough).";
        const string REQ_RYTHMN = "Experimental Settings (currently disabled)";

        public static void Load(ConfigFile cf) {
            _LoadComponentEnabled(cf);
            _LoadByRecipe(cf);
            _LoadAdvancedLabSettings(cf);
        }

        static void _LoadComponentEnabled(ConfigFile cf) {
            const string BUGGY_EXPLAIN = "This setting is to enable you to triage new-update and compatibility issues with this mod.";

            ReBuffer.Enabled |= cf.Bind<bool>(HDR_BUGGY, $"Patch{nameof(EEnabledComponents.AssemblerComponent)}", true, new ConfigDescription(
                $"{BUGGY_EXPLAIN}" +
                $"\nIf true, we'll patch AssemblerComponents (includes Smelters, Refineries, ChemPlants, and more)." +
                $"\nOverwrites: UpdateNeeds, InternalUpdate"
            )).Value ? EEnabledComponents.AssemblerComponent : EEnabledComponents._NONE;

            ReBuffer.Enabled |= cf.Bind<bool>(HDR_BUGGY, $"Patch{nameof(EEnabledComponents.LabComponent)}", true, new ConfigDescription(
                $"{BUGGY_EXPLAIN}" +
                $"\nIf true, we'll patch LabComponents (both Jello and Research modes)." +
                $"\nOverwrites: UpdateOutputToNext, UpdateNeedsAssemble, InternalUpdateAssemble, UpdateNeedsResearch, InternalUpdateResearch"
            )).Value ? EEnabledComponents.LabComponent : EEnabledComponents._NONE;

            ReBuffer.Enabled |= cf.Bind<bool>(HDR_BUGGY, $"Patch{nameof(EEnabledComponents.PowerGenerators)}", true, new ConfigDescription(
                $"{BUGGY_EXPLAIN}" +
                $"\nIf true, we'll patch PowerGenerators (includes all gennies and rayrs)." +
                $"\nOverwrites: GameTick_Gamma" +
                $"\nCurrently Incompatible with JinxOAO's MoreMegastructures (rip)"
            )).Value ? EEnabledComponents.PowerGenerators : EEnabledComponents._NONE;

        }
        static void _LoadByRecipe(ConfigFile cf) {
            var allTypes = ((ERecipeType[])Enum.GetValues(typeof(ERecipeType)));
            var cfgTypes = allTypes
                .Where(type => type != ERecipeType.None
                    && type != ERecipeType.Fractionate//todo... maybe
                    && type != ERecipeType.PhotonStore//todo
                )
                .ToList()
            ;
            lookup_inp = new int[allTypes.Select(rt => (int)rt).Max() + 1];
            lookup_oup = new int[lookup_inp.Length];
            for(int i = 0; i < lookup_inp.Length; i++) {
                var hasCast = Enum.IsDefined(typeof(ERecipeType), i);
                if(!hasCast) {
                    lookup_inp[i] = -1;
                    lookup_oup[i] = -1;
                } else {
                    var cast = (ERecipeType)i;
                    lookup_inp[i] = 4;
                    lookup_oup[i] = 5;
                }
            }

            foreach(var type in cfgTypes) {
                var req = type == ERecipeType.Research ? REQ_LAB : REQ_ASM;
                lookup_inp[(int)type] = cf.Bind<int>(HDR_IN, $"In{type}", 3, new ConfigDescription(
                    $"{req}" +
                    $"\nThe multiplier for the input buffers for {type} ({(int)type}) recipes."
                    , new AcceptableValueRange<int>(1, 100)
                )).Value;
                lookup_oup[(int)type] = cf.Bind<int>(HDR_OUT, $"Out{type}", 5, new ConfigDescription(
                    $"{req}" +
                    $"\nThe multiplier for the output buffers for {type} ({(int)type}) recipes."
                    , new AcceptableValueRange<int>(1, 100)
                )).Value;
            }
        }
        static void _LoadAdvancedLabSettings(ConfigFile cf) {
            JelloAppetite = cf.Bind<int>(HDR_RES, nameof(JelloAppetite), 10, new ConfigDescription(
                $"{REQ_LAB}" +
                "\nThe multiplier for the input buffers for hash-producing (research-mode) labs."
                , new AcceptableValueRange<int>(1, 100)
            )).Value;

            //Dancer Only

            CollapseLabTowers = cf.Bind<bool>(HDR_RYTHMN, nameof(CollapseLabTowers), false, new ConfigDescription(
                $"{REQ_RYTHMN} {REQ_LAB}" +
                $"\nIf true, we'll collapse all of your Lab Towers into just the bottom floor, allowing" +
                " all other labs to basically act as a flashy, overly tall hat." +
                "\nThis doesn't entirely remove them from the entity count, but it does make their tick" +
                " operations notably cheaper."
            )).Value;
        }
        static void _LoadRayRecieverSettings(ConfigFile cf) {
            RayrCataIn = cf.Bind<int>(HDR_RAYR, "Catalyst", 20, new ConfigDescription(
                $"{REQ_RAYR}" +
                "\nThe number of catalysts (ie, Grav Lenses) a rayr should hoard in itself." +
                " Note that catalysts last for 1 minute (assuming 60 ticks per minute)."
                , new AcceptableValueRange<int>(1, 100)
            )).Value;
            RayrProdOut = cf.Bind<int>(HDR_RAYR, "Product", 20, new ConfigDescription(
                $"{REQ_RAYR}" +
                "\nThe number of products (ie, Crit Photons) a rayr should keep ahold of."
                , new AcceptableValueRange<int>(1, 100)
            )).Value;

            RayrPiling = cf.Bind<bool>(HDR_RAYR, "ProductPiling", false, new ConfigDescription(
                $"{REQ_RAYR}" +
                "\nIf set, we'll automatically pile products if we can, up to the max of 4."
            )).Value;

            RayrCataPass = cf.Bind<bool>(HDR_RAYR, "CataPassthrough", false, new ConfigDescription(
                $"{REQ_RAYR}" +
                "\nIf set, AND a ray reciever has AT LEAST 2 + the piled (if piled) quantity, " +
                " AND there's no product whatsoever, we'll pass extra catalysts through." +
                "\nThis feature is only ever usable if you like to use lenses on rayr power generators."
            )).Value;

        }


        internal static int[] lookup_inp;
        internal static int[] lookup_oup;
        internal static int GetInp(ERecipeType @for) => lookup_inp[(int)@for];
        internal static int GetOup(ERecipeType @for) => lookup_oup[(int)@for];
        
        internal static int JelloAppetite { get; private set; }
        internal static bool CollapseLabTowers { get; private set; }

        internal static int RayrCataIn { get; private set; }
        internal static int RayrProdOut { get; private set; }
        internal static bool RayrPiling { get; private set; }
        internal static bool RayrCataPass { get; private set; }
    }
}
