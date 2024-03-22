using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eirshy.DSP.Rythmn.Utilities {
    public class Item : ProtoID<Item> {
        public enum _Roles {
            Factory, Logistics, Gatherer, PowerDelivery, Generator, SphereDepoly, LogTower, Military,
            Acc, Fuel, Sphere, Science, Spray, Other,
            Raw, Refined, Component,
            Bullet, Arty, PlasBall, Missile,
            LogisticsDrone, TroopGround, TroopSpace,
            FogMats,
        }
        public override int Id { get; }
        public _Roles Role { get; }
        public Item(int id, _Roles role) {
            Id = id; Role = role;
        }
        public static IEnumerable<Item> _AllWithRole(_Roles role) => _GetAll().Where(i => i.Role == role);
        public static IEnumerable<Item> _AllWithRole(params _Roles[] roles) {
            var set = new HashSet<_Roles>(roles);
            return _GetAll().Where(i => set.Contains(i.Role));
        }

        #region FACtories
        public static Item FacAss => new Item(2303, _Roles.Factory);
        public static Item FacAss2 => new Item(2304, _Roles.Factory);
        public static Item FacAss3 => new Item(2305, _Roles.Factory);
        public static Item FacAssFog => new Item(2318, _Roles.Factory);
        public static Item FacChem => new Item(2309, _Roles.Factory);
        public static Item FacChem2 => new Item(2317, _Roles.Factory);
        public static Item FacFract => new Item(2314, _Roles.Factory);
        public static Item FacLab => new Item(2901, _Roles.Factory);
        public static Item FacLabFog => new Item(2902, _Roles.Factory);
        public static Item FacPart => new Item(2310, _Roles.Factory);
        public static Item FacRef => new Item(2308, _Roles.Factory);
        public static Item FacSmelt => new Item(2302, _Roles.Factory);
        public static Item FacSmelt2 => new Item(2315, _Roles.Factory);
        public static Item FacSmeltFog => new Item(2319, _Roles.Factory);
        #endregion
        #region LOGistics
        public static Item LogBelt1 => new Item(2001, _Roles.Logistics);
        public static Item LogBelt2 => new Item(2002, _Roles.Logistics);
        public static Item LogBelt3 => new Item(2003, _Roles.Logistics);
        public static Item LogIns => new Item(2011, _Roles.Logistics);
        public static Item LogIns2 => new Item(2012, _Roles.Logistics);
        public static Item LogIns3 => new Item(2013, _Roles.Logistics);
        public static Item LogSplit => new Item(2020, _Roles.Logistics);
        public static Item LogSpray => new Item(2313, _Roles.Logistics);
        public static Item LogBox => new Item(2101, _Roles.Logistics);
        public static Item LogBox2 => new Item(2102, _Roles.Logistics);
        public static Item LogTank => new Item(2106, _Roles.Logistics);
        public static Item LogPiler => new Item(2040, _Roles.Logistics);
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
        public static Item TowChest => new Item(2107, _Roles.LogTower);
        public static Item TowSystem => new Item(2104, _Roles.LogTower);
        public static Item TowPlanet => new Item(2103, _Roles.LogTower);
        public static Item TowOrbit => new Item(2105, _Roles.LogTower);
        #endregion
        #region MILitary
        public static Item MilGauss => new Item(3001, _Roles.Military);
        public static Item MilLaser => new Item(3002, _Roles.Military);
        public static Item MilArtillery => new Item(3003, _Roles.Military);
        public static Item MilPlasBall => new Item(3004, _Roles.Military);
        public static Item MilMissile => new Item(3005, _Roles.Military);
        public static Item MilJammer => new Item(3006, _Roles.Military);
        public static Item MilSignal => new Item(3007, _Roles.Military);
        public static Item MilShield => new Item(3008, _Roles.Military);
        public static Item MilBase => new Item(3009, _Roles.Military);

        #endregion

        #region ACCumulators
        public static Item AccFull => new Item(2207, _Roles.Acc);
        public static Item AccEmpty => new Item(2206, _Roles.Acc);
        #endregion
        #region Fuel Rods
        public static Item FuelAM => new Item(1803, _Roles.Fuel);
        public static Item FuelDT => new Item(1802, _Roles.Fuel);
        public static Item FuelH => new Item(1801, _Roles.Fuel);
        public static Item FuelSA => new Item(1804, _Roles.Fuel);
        
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
        public static Item Spray1 => new Item(1141, _Roles.Spray);
        public static Item Spray2 => new Item(1142, _Roles.Spray);
        public static Item Spray3 => new Item(1143, _Roles.Spray);
        #endregion
        #region Sphere stuff
        public static Item Rocket => new Item(1503, _Roles.Sphere);
        public static Item Sail => new Item(1501, _Roles.Sphere);
        #endregion
        #region Other

        public static Item Foundation => new Item(1131, _Roles.Other);
        public static Item SoilPile => new Item(1099, _Roles.Other);
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
        public static Item CmpThruster0 => new Item(1407, _Roles.Component);
        public static Item CmpThruster1 => new Item(1405, _Roles.Component);
        public static Item CmpThruster2 => new Item(1406, _Roles.Component);
        public static Item CmpStrMat => new Item(1127, _Roles.Component);
        public static Item CmpSuperMag => new Item(1205, _Roles.Component);
        public static Item CmpTitanAlloy => new Item(1107, _Roles.Component);
        public static Item CmpTitanCry => new Item(1118, _Roles.Component);
        public static Item CmpTitanGlass => new Item(1119, _Roles.Component);
        public static Item CmpExploBasic => new Item(1128, _Roles.Component);
        public static Item CmpExplo => new Item(1129, _Roles.Component);
        public static Item CmpExploCry => new Item(1130, _Roles.Component);
        #endregion

        #region Bullet
        public static Item Bullet1 => new Item(1601, _Roles.Bullet);
        public static Item Bullet2 => new Item(1602, _Roles.Bullet);
        public static Item Bullet3 => new Item(1603, _Roles.Bullet);

        #endregion
        #region Artillery (Arty)
        public static Item Arty1 => new Item(1604, _Roles.Arty);
        public static Item Arty2 => new Item(1605, _Roles.Arty);
        public static Item Arty3 => new Item(1606, _Roles.Arty);

        #endregion
        #region Plasma Ball (Pb)
        public static Item Pb1 => new Item(1607, _Roles.PlasBall);
        public static Item Pb2 => new Item(1608, _Roles.PlasBall);

        #endregion
        #region Missile
        public static Item Missile1 => new Item(1609, _Roles.Missile);
        public static Item Missile2 => new Item(1610, _Roles.Missile);
        public static Item Missile3 => new Item(1611, _Roles.Missile);

        #endregion

        #region LOgistics DRone
        public static Item LodrChest => new Item(5003, _Roles.LogisticsDrone);
        public static Item LodrPlanet => new Item(5001, _Roles.LogisticsDrone);
        public static Item LodrSystem => new Item(5002, _Roles.LogisticsDrone);
        #endregion
        #region TROop Ground
        public static Item TrogBasic => new Item(5101, _Roles.TroopGround);
        public static Item TrogSniper => new Item(5102, _Roles.TroopGround);
        public static Item TrogTank => new Item(5103, _Roles.TroopGround);
        #endregion
        #region TROop Space
        public static Item TrosCorv => new Item(5111, _Roles.TroopSpace);
        public static Item TrosDest => new Item(5112, _Roles.TroopSpace);
        #endregion

        #region FOG Mats
        public static Item FogMatrix => new Item(5201, _Roles.FogMats);
        public static Item FogNeuron => new Item(5202, _Roles.FogMats);
        public static Item FogRecomb => new Item(5203, _Roles.FogMats);
        public static Item FogNegentropy => new Item(5204, _Roles.FogMats);
        public static Item FogCore => new Item(5205, _Roles.FogMats);
        public static Item FogShard => new Item(5206, _Roles.FogMats);

        #endregion
    }
}