using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eirshy.DSP.Rythmn.Enums {
    [Flags]
    internal enum EStationStorageSyncKeywords {
        Never = 0,
        Always = 1 << 0,
        //---
        CurOverLimit = 1 << 2,
        CurMaxThreshold = 1 << 3,
        //4-8 open
        TypeCollector = 1 << 8,
        TypeMiner = 1 << 9,
        //9-30 open
        MatchRegistry = 1 << 30,
        _nomatch = 1 << 31,
    }
}
