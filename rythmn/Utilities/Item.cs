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
            Bullet, Shell, PlasBall, Missile, Jamming,
            LogisticsDrone, TroopGround, TroopSpace,
            FogMats,
            Mod_MoreMega, Mod_EmptyFuelRod
        }
        public override int Id { get; }
        protected override string AsString { 
            get => $"{{{Proto.name}}}"; 
            set => _ = value;
        }
        public _Roles Role { get; }
        public Item(int id, _Roles role) {
            Id = id; Role = role;
        }
        public static IEnumerable<Item> _AllWithRole(_Roles role) => _GetAll().Where(i => i.Role == role);
        public static IEnumerable<Item> _AllWithRole(params _Roles[] roles) {
            var set = new HashSet<_Roles>(roles);
            return _GetAll().Where(i => set.Contains(i.Role));
        }

        public int _IsNthOf(params Item[] items) => Array.IndexOf(items, Id);

        #region FACtories
        public static Item FacAss => new(2303, _Roles.Factory);
        public static Item FacAss2 => new(2304, _Roles.Factory);
        public static Item FacAss3 => new(2305, _Roles.Factory);
        public static Item FacAssFog => new(2318, _Roles.Factory);
        public static Item FacChem => new(2309, _Roles.Factory);
        public static Item FacChem2 => new(2317, _Roles.Factory);
        public static Item FacFract => new(2314, _Roles.Factory);
        public static Item FacLab => new(2901, _Roles.Factory);
        public static Item FacLabFog => new(2902, _Roles.Factory);
        public static Item FacPart => new(2310, _Roles.Factory);
        public static Item FacRef => new(2308, _Roles.Factory);
        public static Item FacSmelt => new(2302, _Roles.Factory);
        public static Item FacSmelt2 => new(2315, _Roles.Factory);
        public static Item FacSmeltFog => new(2319, _Roles.Factory);

        #endregion
        #region LOGistics
        public static Item LogBelt1 => new(2001, _Roles.Logistics);
        public static Item LogBelt2 => new(2002, _Roles.Logistics);
        public static Item LogBelt3 => new(2003, _Roles.Logistics);
        public static Item LogIns => new(2011, _Roles.Logistics);
        public static Item LogIns2 => new(2012, _Roles.Logistics);
        public static Item LogIns3 => new(2013, _Roles.Logistics);
        public static Item LogIns4 => new(2014, _Roles.Logistics);
        public static Item LogSplit => new(2020, _Roles.Logistics);
        public static Item LogSpray => new(2313, _Roles.Logistics);
        public static Item LogBox => new(2101, _Roles.Logistics);
        public static Item LogBox2 => new(2102, _Roles.Logistics);
        public static Item LogTank => new(2106, _Roles.Logistics);
        public static Item LogPiler => new(2040, _Roles.Logistics);
        public static Item LogMonitor => new(2030, _Roles.Logistics);

        #endregion
        #region GET raw resources
        public static Item GetOre => new(2301, _Roles.Gatherer);
        public static Item GetOre2 => new(2316, _Roles.Gatherer);
        public static Item GetLiq => new(2306, _Roles.Gatherer);
        public static Item GetOil => new(2307, _Roles.Gatherer);

        #endregion
        #region POWer delivery
        public static Item PowBasic => new(2201, _Roles.PowerDelivery);
        public static Item PowLong => new(2202, _Roles.PowerDelivery);
        public static Item PowSat => new(2212, _Roles.PowerDelivery);

        #endregion
        #region power GENerators
        public static Item GenTherm => new(2204, _Roles.Generator);
        public static Item GenEx => new(2209, _Roles.Generator);
        public static Item GenFusion => new(2211, _Roles.Generator);
        public static Item GenGeo => new(2213, _Roles.Generator);
        public static Item GenRayr => new(2208, _Roles.Generator);
        public static Item GenSol => new(2205, _Roles.Generator);
        public static Item GenMSun => new(2210, _Roles.Generator);
        public static Item GenWind => new(2203, _Roles.Generator);

        #endregion
        #region SPHere builders
        public static Item SphLaunch => new(2312, _Roles.SphereDepoly);
        public static Item SphRail => new(2311, _Roles.SphereDepoly);

        #endregion
        #region TOWers
        public static Item TowChest => new(2107, _Roles.LogTower);
        public static Item TowSystem => new(2104, _Roles.LogTower);
        public static Item TowPlanet => new(2103, _Roles.LogTower);
        public static Item TowOrbit => new(2105, _Roles.LogTower);

        #endregion
        #region MILitary
        public static Item MilGauss => new(3001, _Roles.Military);
        public static Item MilLaser => new(3002, _Roles.Military);
        public static Item MilArtillery => new(3003, _Roles.Military);
        public static Item MilPlasBall => new(3004, _Roles.Military);
        public static Item MilPlasSR => new(3010, _Roles.Military);
        public static Item MilMissile => new(3005, _Roles.Military);
        public static Item MilJammer => new(3006, _Roles.Military);
        public static Item MilSignal => new(3007, _Roles.Military);
        public static Item MilShield => new(3008, _Roles.Military);
        public static Item MilBase => new(3009, _Roles.Military);

        #endregion

        #region ACCumulators
        public static Item AccFull => new(2207, _Roles.Acc);
        public static Item AccEmpty => new(2206, _Roles.Acc);

        #endregion
        #region Fuel Rods
        public static Item FuelAM => new(1803, _Roles.Fuel);
        public static Item FuelDT => new(1802, _Roles.Fuel);
        public static Item FuelH => new(1801, _Roles.Fuel);
        public static Item FuelSA => new(1804, _Roles.Fuel);
        
        #endregion
        #region SCIence cubes (by color)
        /// <summary>
        /// Electro-Magnetism
        /// </summary>
        public static Item SciBlue => new(6001, _Roles.Science);
        /// <summary>
        /// Energy
        /// </summary>
        public static Item SciRed => new(6002, _Roles.Science);
        /// <summary>
        /// Structure
        /// </summary>
        public static Item SciYellow => new(6003, _Roles.Science);
        /// <summary>
        /// Information
        /// </summary>
        public static Item SciPurple => new(6004, _Roles.Science);
        /// <summary>
        /// Gravity
        /// </summary>
        public static Item SciGreen => new(6005, _Roles.Science);
        /// <summary>
        /// Universe
        /// </summary>
        public static Item SciWhite => new(6006, _Roles.Science);
        #endregion
        #region Spray-paint
        public static Item Spray1 => new(1141, _Roles.Spray);
        public static Item Spray2 => new(1142, _Roles.Spray);
        public static Item Spray3 => new(1143, _Roles.Spray);
        #endregion
        #region Sphere stuff
        public static Item Rocket => new(1503, _Roles.Sphere);
        public static Item Sail => new(1501, _Roles.Sphere);

        #endregion
        #region Other

        public static Item Foundation => new(1131, _Roles.Other);
        public static Item SoilPile => new(1099, _Roles.Other);
        public static Item Warp => new(1210, _Roles.Other);

        #endregion

        #region RAW resources
        public static Item RawCoal => new(1006, _Roles.Raw);
        public static Item RawCopper => new(1002, _Roles.Raw);
        public static Item RawCritPho => new(1208, _Roles.Raw);
        public static Item RawOil => new(1007, _Roles.Raw);
        public static Item RawDeu => new(1121, _Roles.Raw);
        public static Item RawFireIce => new(1011, _Roles.Raw);
        public static Item RawFracSili => new(1013, _Roles.Raw);
        public static Item RawHyd => new(1120, _Roles.Raw);
        public static Item RawIron => new(1001, _Roles.Raw);
        public static Item RawKimber => new(1012, _Roles.Raw);
        public static Item RawWood => new(1030, _Roles.Raw);
        public static Item RawOpGCry => new(1014, _Roles.Raw);
        public static Item RawOrgCry => new(1117, _Roles.Raw);
        public static Item RawPlants => new(1031, _Roles.Raw);
        public static Item RawSilic => new(1003, _Roles.Raw);
        public static Item RawSpinCry => new(1015, _Roles.Raw);
        public static Item RawStone => new(1005, _Roles.Raw);
        public static Item RawSulfAcid => new(1116, _Roles.Raw);
        public static Item RawTitan => new(1004, _Roles.Raw);
        public static Item RawUniMag => new(1016, _Roles.Raw);
        public static Item RawWater => new(1000, _Roles.Raw);

        #endregion
        #region REFined resources
        public static Item RefAntiMat => new(1122, _Roles.Refined);
        public static Item RefCarbTube => new(1124, _Roles.Refined);
        public static Item RefCopper => new(1104, _Roles.Refined);
        public static Item RefSiliCry => new(1113, _Roles.Refined);
        public static Item RefDiamond => new(1112, _Roles.Refined);
        public static Item RefEnGraph => new(1109, _Roles.Refined);
        public static Item RefGlass => new(1110, _Roles.Refined);
        public static Item RefGraph => new(1123, _Roles.Refined);
        public static Item RefSilic => new(1105, _Roles.Refined);
        public static Item RefIron => new(1101, _Roles.Refined);
        public static Item RefMag => new(1102, _Roles.Refined);
        public static Item RefOil => new(1114, _Roles.Refined);
        public static Item RefStone => new(1108, _Roles.Refined);
        public static Item RefTitan => new(1106, _Roles.Refined);

        #endregion
        #region CMP - Components
        public static Item CmpAnniConst => new(1403, _Roles.Component);
        public static Item CmpCasmir => new(1126, _Roles.Component);
        public static Item CmpCircFe => new(1301, _Roles.Component);
        public static Item CmpDSComp => new(1502, _Roles.Component);
        public static Item CmpEMotor => new(1203, _Roles.Component);
        public static Item CmpETurb => new(1204, _Roles.Component);
        public static Item CmpFrame => new(1125, _Roles.Component);
        public static Item CmpGear => new(1201, _Roles.Component);
        public static Item CmpLens => new(1209, _Roles.Component);
        public static Item CmpMag2 => new(1202, _Roles.Component);
        public static Item CmpCircSi => new(1302, _Roles.Component);
        public static Item CmpPartBroad => new(1402, _Roles.Component);
        public static Item CmpPartCont => new(1206, _Roles.Component);
        public static Item CmpPhotoCom => new(1404, _Roles.Component);
        public static Item CmpPlaneFilt => new(1304, _Roles.Component);
        public static Item CmpPlasmaEx => new(1401, _Roles.Component);
        public static Item CmpPlastic => new(1115, _Roles.Component);
        public static Item CmpPrism => new(1111, _Roles.Component);
        public static Item CmpProc => new(1303, _Roles.Component);
        public static Item CmpQChip => new(1305, _Roles.Component);
        public static Item CmpSteel => new(1103, _Roles.Component);
        public static Item CmpThruster0 => new(1407, _Roles.Component);
        public static Item CmpThruster1 => new(1405, _Roles.Component);
        public static Item CmpThruster2 => new(1406, _Roles.Component);
        public static Item CmpStrMat => new(1127, _Roles.Component);
        public static Item CmpSuperMag => new(1205, _Roles.Component);
        public static Item CmpTitanAlloy => new(1107, _Roles.Component);
        public static Item CmpTitanCry => new(1118, _Roles.Component);
        public static Item CmpTitanGlass => new(1119, _Roles.Component);
        public static Item CmpExploBasic => new(1128, _Roles.Component);
        public static Item CmpExplo => new(1129, _Roles.Component);
        public static Item CmpExploCry => new(1130, _Roles.Component);

        #endregion

        #region Bullet
        public static Item Bullet1 => new(1601, _Roles.Bullet);
        public static Item Bullet2 => new(1602, _Roles.Bullet);
        public static Item Bullet3 => new(1603, _Roles.Bullet);

        #endregion
        #region Artillery Shell (Shell)
        public static Item Shell1 => new(1604, _Roles.Shell);
        public static Item Shell2 => new(1605, _Roles.Shell);
        public static Item Shell3 => new(1606, _Roles.Shell);

        #endregion
        #region Plasma Ball
        public static Item PlasBall1 => new(1607, _Roles.PlasBall);
        public static Item PlasBall2 => new(1608, _Roles.PlasBall);

        #endregion
        #region Missile
        public static Item Missile1 => new(1609, _Roles.Missile);
        public static Item Missile2 => new(1610, _Roles.Missile);
        public static Item Missile3 => new(1611, _Roles.Missile);

        #endregion
        #region Jamming
        public static Item Jamming1 => new(1612, _Roles.Jamming);
        public static Item Jamming2 => new(1613, _Roles.Jamming);

        #endregion

        #region LOgistics DRone
        public static Item LodrChest => new(5003, _Roles.LogisticsDrone);
        public static Item LodrPlanet => new(5001, _Roles.LogisticsDrone);
        public static Item LodrSystem => new(5002, _Roles.LogisticsDrone);

        #endregion
        #region TROop Ground
        public static Item TrogBasic => new(5101, _Roles.TroopGround);
        public static Item TrogSniper => new(5102, _Roles.TroopGround);
        public static Item TrogTank => new(5103, _Roles.TroopGround);

        #endregion
        #region TROop Space
        public static Item TrosCorv => new(5111, _Roles.TroopSpace);
        public static Item TrosDest => new(5112, _Roles.TroopSpace);

        #endregion

        #region FOG Mats
        public static Item FogMatrix => new(5201, _Roles.FogMats);
        public static Item FogNeuron => new(5202, _Roles.FogMats);
        public static Item FogRecomb => new(5203, _Roles.FogMats);
        public static Item FogNegentropy => new(5204, _Roles.FogMats);
        public static Item FogCore => new(5205, _Roles.FogMats);
        public static Item FogShard => new(5206, _Roles.FogMats);

        #endregion

        #region Mod_MoreMega
        public static Item MM_Mfc => new(9500, _Roles.Mod_MoreMega);
        public static Item MM_InterAssTower => new(9512, _Roles.LogTower);

        public static Item MM_RocketMatDecomp => new Item(9488, _Roles.Sphere);
        public static Item MM_RocketScience => new Item(9489, _Roles.Sphere);
        public static Item MM_RocketWarp => new Item(9490, _Roles.Sphere);
        public static Item MM_RocketInterAss => new Item(9491, _Roles.Sphere);
        public static Item MM_RocketCrystal => new Item(9492, _Roles.Sphere);
        public static Item MM_RocketCannon => new Item(9510, _Roles.Sphere);

        public static Item MM_GravGen => new Item(9480, _Roles.Component);
        public static Item MM_PlaneConstRing => new Item(9481, _Roles.Component);
        public static Item MM_GravDrill => new Item(9482, _Roles.Component);
        public static Item MM_TunExcite => new Item(9483, _Roles.Component);
        public static Item MM_ResDisc => new Item(9484, _Roles.Component);
        public static Item MM_PhoProbe => new Item(9485, _Roles.Component);
        public static Item MM_Qomputer => new Item(9486, _Roles.Component);
        public static Item MM_InterAssComp => new Item(9487, _Roles.Component);

        public static Item MM_FFieldGen => new Item(9503, _Roles.Component);
        public static Item MM_CompCry => new Item(9504, _Roles.Component);
        public static Item MM_ElectroForceSup => new Item(9505, _Roles.Component);
        public static Item MM_GluonGen => new Item(9506, _Roles.Component);
        public static Item MM_OverloadDevice => new Item(9507, _Roles.Component);
        public static Item MM_FlowFrame => new Item(9508, _Roles.Component);
        public static Item MM_CannonComp => new Item(9509, _Roles.Component);

        public static Item MM_Droplet => new Item(9511, _Roles.Mod_MoreMega);

        public static Item MM_MatDeRayrIron => new Item(9493, _Roles.Mod_MoreMega);
        public static Item MM_MatDeRayrCopper => new Item(9494, _Roles.Mod_MoreMega);
        public static Item MM_MatDeRayrSili => new Item(9495, _Roles.Mod_MoreMega);
        public static Item MM_MatDeRayrTitan => new Item(9496, _Roles.Mod_MoreMega);
        public static Item MM_MatDeRayrUnmag => new Item(9497, _Roles.Mod_MoreMega);
        public static Item MM_CryRayrCas => new Item(9498, _Roles.Mod_MoreMega);
        public static Item MM_InterAssRayrMfc => new Item(9499, _Roles.Mod_MoreMega);
        public static Item MM_MatDeGraphite => new Item(9501, _Roles.Mod_MoreMega);
        public static Item MM_CryRayrOptical => new Item(9502, _Roles.Mod_MoreMega);



        #endregion
        #region Mod_EmptyFuelRods

        public static Item EmptyFuelDT => new Item(9451, _Roles.Mod_EmptyFuelRod);
        public static Item EmptyFuelAM => new Item(9452, _Roles.Mod_EmptyFuelRod);

        #endregion
    }
}