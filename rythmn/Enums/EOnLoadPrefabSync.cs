using System;

namespace Eirshy.DSP.Rythmn.Enums {

    [Flags]
    public enum EOnLoadPrefabSync {
        _NONE = 0, _ALL_AVAILABLE = -1,
        #region Factory System (0-8)

        Factory_ALL = FactoryAssembler | FactoryEjector | FactoryFractionator | FactoryInserter | FactoryLab | FactoryMiner | FactorySilo,
        FactoryAssembler = 1 << 0,
        FactoryEjector = 1 << 1,
        FactoryFractionator = 1 << 2,
        FactoryInserter = 1 << 3,
        FactoryLab = 1 << 4,
        FactoryMiner = 1 << 5,
        FactorySilo = 1 << 6,
        //unused: 7-8

        #endregion
        #region Power System (9-15)

        Power_ALL = PowerAccumulator | PowerConsumer | PowerExchanger | PowerGenerator | PowerNode,
        PowerAccumulator = 1 << 9,
        PowerConsumer = 1 << 10,
        PowerExchanger = 1 << 11,
        PowerGenerator = 1 << 12,
        PowerNode = 1 << 13,
        //unused 14-15

        #endregion
        #region Cargo Traffic (16-22)

        Cargo_ALL = CargoBelt | CargoSplitter | CargoMonitor | CargoSpraycoater | CargoPiler,
        CargoBelt = 1 << 16,
        CargoSplitter = 1 << 17,
        CargoMonitor = 1 << 18,
        CargoSpraycoater = 1 << 19,
        CargoPiler = 1 << 20,
        //unused: 21-22

        #endregion
        #region Planet Transport (22-29)

        Transport_ALL = TransportStation_Harvesting | TransportStation_Storage | TransportStation_Drones,
        TransportStation_Harvesting = 1 << 22,
        TransportStation_Storage = 1 << 23,
        TransportStation_Drones = 1 << 24,

        #endregion
        #region Other (30, 31)
        
        //unused: 30
        Other_Recipies = 1 << 31,

        #endregion
    }
}
