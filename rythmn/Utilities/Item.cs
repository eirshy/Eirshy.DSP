using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eirshy.DSP.Rythmn.Utilities {
    public class Item : ProtoID<Item> {
        public enum _Roles {
            Factory, Logistics, Gatherer, PowerDelivery, Generator, SphereDepoly, Tower,
            Acc, Drone, Fuel, Sphere, Science, Spray, Other,
            Raw, Refined, Component,
        }
        public override int Id { get; }
        public _Roles Role { get; }
        public Item(int id, _Roles role) {
            Id = id; Role = role;
        }
        public static IEnumerable<Item> _AllWithRole(_Roles role) => _GetAll().Where(i => i.Role == role);

        #region FACtories
        public static Item FacAss => new Item(2303, _Roles.Factory);
        public static Item FacAss2 => new Item(2304, _Roles.Factory);
        public static Item FacAss3 => new Item(2305, _Roles.Factory);
        public static Item FacChem => new Item(2309, _Roles.Factory);
        public static Item FacFract => new Item(2314, _Roles.Factory);
        public static Item FacLab => new Item(2901, _Roles.Factory);
        public static Item FacPart => new Item(2310, _Roles.Factory);
        public static Item FacRef => new Item(2308, _Roles.Factory);
        public static Item FacSmelt => new Item(2302, _Roles.Factory);
        public static Item FacSmelt2 => new Item(2315, _Roles.Factory);
        #endregion
        #region LOGistics
        public static Item LogBelt3 => new Item(2003, _Roles.Logistics);
        public static Item LogIns => new Item(2011, _Roles.Logistics);
        public static Item LogIns2 => new Item(2012, _Roles.Logistics);
        public static Item LogIns3 => new Item(2013, _Roles.Logistics);
        public static Item LogSplit => new Item(2020, _Roles.Logistics);
        public static Item LogSpray => new Item(2313, _Roles.Logistics);
        public static Item LogBox => new Item(2101, _Roles.Logistics);
        public static Item LogBox2 => new Item(2102, _Roles.Logistics);
        public static Item LogTank => new Item(2016, _Roles.Logistics);
        public static Item LogPiler => new Item(2106, _Roles.Logistics);
        public static Item LogMonitor => new Item(2030, _Roles.Logistics);
        #endregion
        #region GET raw resources
        public static Item GetOre => new Item(2301, _Roles.Gatherer);
        public static Item GetOre2 => new Item(2316, _Roles.Gatherer);
        public static Item GetLiq => new Item(2306, _Roles.Gatherer);
        public static Item GetOil => new Item(2307, _Roles.Gatherer);
        #endregion
        #region POWer delivery
        public static Item PowBasic => new Item(2201, _Roles.PowerDelivery);
        public static Item PowLong => new Item(2202, _Roles.PowerDelivery);
        public static Item PowSat => new Item(2212, _Roles.PowerDelivery);
        #endregion
        #region power GENerators
        public static Item GenTherm => new Item(2204, _Roles.Generator);
        public static Item GenEx => new Item(2209, _Roles.Generator);
        public static Item GenFusion => new Item(2211, _Roles.Generator);
        public static Item GenGeo => new Item(2213, _Roles.Generator);
        public static Item GenRayr => new Item(2208, _Roles.Generator);
        public static Item GenSol => new Item(2205, _Roles.Generator);
        public static Item GenMSun => new Item(2210, _Roles.Generator);
        public static Item GenWind => new Item(2203, _Roles.Generator);
        #endregion
        #region SPHere builders
        public static Item SphLaunch => new Item(2312, _Roles.SphereDepoly);
        public static Item SphRail => new Item(2311, _Roles.SphereDepoly);
        #endregion
        #region TOWers
        public static Item TowInter => new Item(2104, _Roles.Tower);
        public static Item TowPlanet => new Item(2103, _Roles.Tower);
        public static Item TowOrbit => new Item(2105, _Roles.Tower);
        #endregion

        #region ACCumulators
        public static Item AccFull => new Item(2207, _Roles.Acc);
        public static Item AccEmpty => new Item(2206, _Roles.Acc);
        #endregion
        #region Drones
        public static Item DroneP => new Item(5001, _Roles.Drone);
        public static Item DroneI => new Item(5002, _Roles.Drone);
        #endregion
        #region Fuel Rods
        public static Item FuelAM => new Item(1803, _Roles.Fuel);
        public static Item FuelDT => new Item(1802, _Roles.Fuel);
        public static Item FuelH => new Item(1801, _Roles.Fuel);
        #endregion
        #region SCIence cubes (by color)
        /// <summary>
        /// Electro-Magnetism
        /// </summary>
        public static Item SciBlue => new Item(6001, _Roles.Science);
        /// <summary>
        /// Energy
        /// </summary>
        public static Item SciRed => new Item(6002, _Roles.Science);
        /// <summary>
        /// Structure
        /// </summary>
        public static Item SciYellow => new Item(6003, _Roles.Science);
        /// <summary>
        /// Information
        /// </summary>
        public static Item SciPurple => new Item(6004, _Roles.Science);
        /// <summary>
        /// Gravity
        /// </summary>
        public static Item SciGreen => new Item(6005, _Roles.Science);
        /// <summary>
        /// Universe
        /// </summary>
        public static Item SciWhite => new Item(6006, _Roles.Science);
        #endregion
        #region Spray-paint
        public static Item Spray => new Item(1141, _Roles.Spray);
        public static Item Spray2 => new Item(1142, _Roles.Spray);
        public static Item Spray3 => new Item(1143, _Roles.Spray);
        #endregion
        #region Sphere stuff
        public static Item Rocket => new Item(1503, _Roles.Sphere);
        public static Item Sail => new Item(1501, _Roles.Sphere);
        #endregion
        #region Other

        public static Item Foundation => new Item(1131, _Roles.Other);
        public static Item Warp => new Item(1210, _Roles.Other);

        #endregion

        #region RAW resources
        public static Item RawCoal => new Item(1006, _Roles.Raw);
        public static Item RawCopper => new Item(1002, _Roles.Raw);
        public static Item RawCritPho => new Item(1208, _Roles.Raw);
        public static Item RawOil => new Item(1007, _Roles.Raw);
        public static Item RawDeu => new Item(1121, _Roles.Raw);
        public static Item RawFireIce => new Item(1011, _Roles.Raw);
        public static Item RawFracSili => new Item(1013, _Roles.Raw);
        public static Item RawHyd => new Item(1120, _Roles.Raw);
        public static Item RawIron => new Item(1001, _Roles.Raw);
        public static Item RawKimber => new Item(1012, _Roles.Raw);
        public static Item RawWood => new Item(1030, _Roles.Raw);
        public static Item RawOpGCry => new Item(1014, _Roles.Raw);
        public static Item RawOrgCry => new Item(1117, _Roles.Raw);
        public static Item RawPlants => new Item(1031, _Roles.Raw);
        public static Item RawSilic => new Item(1003, _Roles.Raw);
        public static Item RawSpinCry => new Item(1015, _Roles.Raw);
        public static Item RawStone => new Item(1005, _Roles.Raw);
        public static Item RawSulfAcid => new Item(1116, _Roles.Raw);
        public static Item RawTitan => new Item(1004, _Roles.Raw);
        public static Item RawUniMag => new Item(1016, _Roles.Raw);
        public static Item RawWater => new Item(1000, _Roles.Raw);
        #endregion
        #region REFined resources
        public static Item RefAntiMat => new Item(1122, _Roles.Refined);
        public static Item RefCarbTube => new Item(1124, _Roles.Refined);
        public static Item RefCopper => new Item(1104, _Roles.Refined);
        public static Item RefSiliCry => new Item(1113, _Roles.Refined);
        public static Item RefDiamond => new Item(1112, _Roles.Refined);
        public static Item RefEnGraph => new Item(1109, _Roles.Refined);
        public static Item RefGlass => new Item(1110, _Roles.Refined);
        public static Item RefGraph => new Item(1123, _Roles.Refined);
        public static Item RefSilic => new Item(1105, _Roles.Refined);
        public static Item RefIron => new Item(1101, _Roles.Refined);
        public static Item RefMag => new Item(1102, _Roles.Refined);
        public static Item RefOil => new Item(1114, _Roles.Refined);
        public static Item RefStone => new Item(1108, _Roles.Refined);
        public static Item RefTitan => new Item(1106, _Roles.Refined);
        #endregion
        #region CMP - Components
        public static Item CmpAnniConst => new Item(1403, _Roles.Component);
        public static Item CmpCasmir => new Item(1126, _Roles.Component);
        public static Item CmpCircFe => new Item(1301, _Roles.Component);
        public static Item CmpDSComp => new Item(1502, _Roles.Component);
        public static Item CmpEMotor => new Item(1203, _Roles.Component);
        public static Item CmpETurb => new Item(1204, _Roles.Component);
        public static Item CmpFrame => new Item(1125, _Roles.Component);
        public static Item CmpGear => new Item(1201, _Roles.Component);
        public static Item CmpLens => new Item(1209, _Roles.Component);
        public static Item CmpMag2 => new Item(1202, _Roles.Component);
        public static Item CmpCircSi => new Item(1302, _Roles.Component);
        public static Item CmpPartBroad => new Item(1402, _Roles.Component);
        public static Item CmpPartCont => new Item(1206, _Roles.Component);
        public static Item CmpPhotoCom => new Item(1404, _Roles.Component);
        public static Item CmpPlaneFilt => new Item(1304, _Roles.Component);
        public static Item CmpPlasmaEx => new Item(1401, _Roles.Component);
        public static Item CmpPlastic => new Item(1115, _Roles.Component);
        public static Item CmpPrism => new Item(1111, _Roles.Component);
        public static Item CmpProc => new Item(1303, _Roles.Component);
        public static Item CmpQChip => new Item(1305, _Roles.Component);
        public static Item CmpSteel => new Item(1103, _Roles.Component);
        public static Item CmpThruster1 => new Item(1405, _Roles.Component);
        public static Item CmpThruster2 => new Item(1406, _Roles.Component);
        public static Item CmpStrMat => new Item(1127, _Roles.Component);
        public static Item CmpSuperMag => new Item(1205, _Roles.Component);
        public static Item CmpTitanAlloy => new Item(1107, _Roles.Component);
        public static Item CmpTitanCry => new Item(1118, _Roles.Component);
        public static Item CmpTitanGlass => new Item(1119, _Roles.Component);
        #endregion
    }
}