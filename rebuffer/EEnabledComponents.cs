using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eirshy.DSP.ReBuffer {

    //I will *militantly* keep these values the same.

    [Flags]
    public enum EEnabledComponents {
        _NONE = 0, _ALL = -1,

        /// <summary>
        /// Overwrites: UpdateNeeds, InternalUpdate
        /// </summary>
        AssemblerComponent = 1 << 00,

        /// <summary>
        /// Overwrites: UpdateOutputToNext, UpdateNeedsAssemble, InternalUpdateAssemble, UpdateNeedsResearch, InternalUpdateResearch
        /// </summary>
        LabComponent = 1 << 01,
        /// <summary>
        /// Includes: UpdateOutputToNext, UpdateNeedsAssemble, InternalUpdateAssemble, UpdateNeedsResearch, InternalUpdateResearch
        /// </summary>
        LabDancers = LabComponent & 1 << 02,

        /// <summary>
        /// Includes: GameTick_Gamma
        /// </summary>
        PowerGenerators = 1 << 03,
    }
}
