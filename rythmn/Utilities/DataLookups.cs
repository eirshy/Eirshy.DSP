using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Eirshy.DSP.Rythmn.Utilities {
    /// <summary>
    /// Various 'standardized' lookups for current values. 
    /// </summary>
    /// <remarks>
    /// The term "PrefabUnit" means the whatever "units" the prefabDesc uses.
    /// <br />Ex, if you wanted to know how fast a Tier 3 Assembler worked, you would take 
    ///   its <c>prefabDesc.assemblerSpeed</c> and divide it by <c>PrefabUnit_Per_One__AssemblerSpeed</c>
    /// <br />As these are hot-loaded from the current game prefabs when asked for, 
    ///   they represent only the *current* value which they're based on.
    /// <br />Note that you should always cache these values yourself instead of using them inside
    ///   a visitor or tick hook.
    /// </remarks>
    public static class DataLookups {
        /// <summary>
        /// Based on The value of a vanilla tier 1 smelter's assemblerSpeed
        /// </summary>
        public static int PrefabUnit_Per_One__AssemblerSpeed => Item.FacSmelt.Proto.prefabDesc.assemblerSpeed;
        /// <summary>
        /// Based on the value of a vanilla tier 1 lab's researchSpeed
        /// <Br />Note that research labs currently completely ignore this value.
        /// </summary>
        public static float PrefabUnit_Per_One__ResearchSpeed => Item.FacLab.Proto.prefabDesc.labResearchSpeed;


        /// <summary>
        /// Additive Multiplier; 0.0 would be "no change", 1.0 would be "double"
        /// </summary>
        public static double Proli_MaxProd => Cargo.incTableMilli[Cargo.kSprayIncMax];
        /// <summary>
        /// Additive Multiplier; 0.0 would be "no change", 1.0 would be "double"
        /// </summary>
        public static double Proli_MaxSpeed => Cargo.accTableMilli[Cargo.kSprayIncMax];
        /// <summary>
        /// Multiplier; 1.0 would be "no change", 2.0 would be "double"
        /// </summary>
        public static double Proli_MaxPowerMult => Cargo.powerTableRatio[Cargo.kSprayIncMax];


        /// <summary>
        /// Currently hard-coded at 6 until I can find a point in the code where it's set.
        /// </summary>
        public static int PrefabUnit_MultTo_ItemsPerSecond__BeltSpeed => 6;
        /// <summary>
        /// Currently hard-coded at 4 until I can find a point in the code where it's set.
        /// </summary>
        public static int Belt_PhysicalMaxStackSize => 4;
        /// <summary>
        /// The current highest <c>prefabDesc.beltSpeed</c> on all <c>prefabDesc.isBelt</c> entities.
        /// </summary>
        public static int Belt_MaxSpeed_PrefabUnit => LDB.items.dataArray.Where(ip => ip.prefabDesc.isBelt).Select(ip => ip.prefabDesc.beltSpeed).Max();
        /// <summary>
        /// <c>Belt_MaxSpeed_PrefabUnit</c> * <c>PrefabUnit_MultTo_ItemsPerSecond__BeltSpeed</c>
        /// <br />But like, waaaay less typing.
        /// </summary>
        public static double Belt_MaxSpeed_ItemsPerSecond => Belt_MaxSpeed_PrefabUnit * PrefabUnit_MultTo_ItemsPerSecond__BeltSpeed;


    }
}
