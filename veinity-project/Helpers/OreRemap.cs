using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Eirshy.DSP.VeinityProject.Helpers {

    internal struct OreRemap {
        /// <remarks>
        /// People's settings depend on this not changing
        /// </remarks>
        const int PER_OUTOF = 60;
        const int SUBKEY_ANY = 0x0000_ffff;
        public static bool HasRemap { get; private set; }

        public readonly int MinerID;
        public readonly int OreID;
        public readonly int Per;
        public readonly int ProductID;

        public int Key => KeyFrom(MinerID, OreID);
        public int Ore2Prod(int ore) => ore * PER_OUTOF / Per;
        public int Prod2Ore(int product) => product * Per / PER_OUTOF;
        internal static int CalcOre2Product(int qtyOre, int qtyProduct) {
            return qtyOre * PER_OUTOF / qtyProduct;
        }

        /// <summary>
        /// NOTE: We auto-trunc MinerID and OreID to 16 bits!
        /// This means -1 is 65,535 !!
        /// </summary>
        public OreRemap(int mid, int oid, int per, int prod) {
            MinerID = mid & SUBKEY_ANY;
            OreID = oid & SUBKEY_ANY;
            Per = per;
            ProductID = prod;
        }
        private OreRemap(bool ignored) {
            MinerID = SUBKEY_ANY;
            OreID = SUBKEY_ANY;
            Per = PER_OUTOF;
            ProductID = 0;
        }
        public bool IsDefault => ProductID == 0;

        #region Serialze, Deserialize

        /// <summary>
        /// MineID map OreID x Per12 => ProdID x 12
        /// <br /><c>{MinerID}m:{OreID}x{Per}=>{ProductID}x{PER_OUTOF}</c>
        /// </summary>
        public string Serialize() => $"{MinerID}m:{OreID}x{Per}=>{ProductID}x{PER_OUTOF}";
        private static readonly Regex _deser = new(
                @"^(\d+)m:(\d+)x(\d+)=>(\d+)x\d+$"
            );
        public static string Serialize(OreRemap[] from) => string.Join(" ", from.Select(rm => rm.Serialize()));
        public static OreRemap[] Deserialize(string from) {
            return (new Regex(@"[ .;,|-]+")).Split(from.Trim().ToLower())
                .Select(s => _deser.Match(s))
                .Where(m => m.Success)
                .Select(m => new OreRemap(
                    //groups index at 1
                    int.Parse(m.Groups[1].Value)
                    , int.Parse(m.Groups[2].Value)
                    , int.Parse(m.Groups[3].Value)
                    , int.Parse(m.Groups[4].Value)
                ))
                .ToArray()
            ;
        }

        #endregion
        #region Tracking and Lookup

        static int KeyFrom(int MinerID, int OreID) => MinerID << 16 | OreID;
        static ConcurrentDictionary<int, OreRemap> _register = new();
        internal static void Register(int mid, int oid, int per, int prod) {
            if(_baked != null) throw new InvalidOperationException("Remap has already been baked!");
            HasRemap = true;
            var remap = new OreRemap(mid, oid, per, prod);
            _register[remap.Key] = remap;
        }

        static Dictionary<int, OreRemap> _baked = null;
        public static void Bake() {
            //want to exact it, so enumerate first
            var raw = _register.ToList();
            _baked = new Dictionary<int, OreRemap>(raw.Count);
            foreach(var kvp in raw) {
                _baked.Add(kvp.Key, kvp.Value);
                VeinityProject.Logs.LogMessage($"Baked: {kvp.Value.Serialize()}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static OreRemap Get(int MinerID, int OreID) {
            var key = KeyFrom(MinerID, OreID);
            if(_baked.ContainsKey(key)) return _baked[key];
            //key = KeyFrom(MinerID, SUBKEY_ANY);
            //if(_baked.Value.ContainsKey(key)) return _baked.Value[key];
            //key = KeyFrom(SUBKEY_ANY, OreID);
            //if(_baked.Value.ContainsKey(key)) return _baked.Value[key];
            return new OreRemap();
        }

        #endregion
    }
}
