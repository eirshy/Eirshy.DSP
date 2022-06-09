using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Eirshy.DSP.Rythmn.Utilities {

    /// <summary>
    /// Simple descriptor of a StationStorage value based on Remote/Local config and optionally ItemID
    /// </summary>
    public struct StationStorageMatch {
        #region Code, our single-char listing
        
        public enum Code {
            ALL = 'a', NONE = 'A',
            Supply = 's', Demand = 'd', Storage = 'x',
            NotSupply = 'S', NotDemand = 'D', NotStorage = 'X',
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCode(in char c) {
            switch((Code)c) {
                default: return false;
                case Code.ALL:
                case Code.NONE:
                case Code.Supply:
                case Code.Demand:
                case Code.Storage:
                case Code.NotSupply:
                case Code.NotDemand:
                case Code.NotStorage:
                    return true;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CheckCode(in Code match, in ELogisticStorage store) {
            switch(match) {
                default: throw new NotImplementedException();
                case Code.ALL: return true;
                case Code.NONE: return false;
                case Code.Supply: return store == ELogisticStorage.Supply;
                case Code.NotSupply: return store != ELogisticStorage.Supply;
                case Code.Demand: return store == ELogisticStorage.Demand;
                case Code.NotDemand: return store != ELogisticStorage.Demand;
                case Code.Storage: return store == ELogisticStorage.None;
                case Code.NotStorage: return store != ELogisticStorage.None;
            }
        }
        
        #endregion

        public static StationStorageMatch ERRORED { get; } = new StationStorageMatch(true);

        public readonly Code Remote;
        public readonly Code Local;
        public readonly bool HasItemID;
        public readonly int ItemID;
        public readonly bool Errored;

        private StationStorageMatch(bool errored) {
            Local = Code.ALL;
            Remote = Code.ALL;
            HasItemID = false;
            ItemID = 0;
            Errored = true;
        }
        public StationStorageMatch(Code rem, Code loc) {
            Local = loc;
            Remote = rem;
            HasItemID = false;
            ItemID = 0;
            Errored = false;
        }
        public StationStorageMatch(Code rem, Code loc, int id) {
            Local = loc;
            Remote = rem;
            HasItemID = true;
            ItemID = id;
            Errored = false;
        }

        /// <summary>
        /// {Remote}{Local}[ItemID]
        /// </summary>
        public static StationStorageMatch FromString(string s) {
            if(s.Length == 2) {
                if(IsCode(s[0]) && IsCode(s[1])) return new StationStorageMatch((Code)s[0], (Code)s[1]);
            } else if(s.Length > 2) {
                if(IsCode(s[0]) && IsCode(s[1]) && int.TryParse(s.Substring(2), out var id)){
                    return new StationStorageMatch((Code)s[0], (Code)s[1], id);
                }
            }
            return ERRORED;
        }
        /// <summary>
        /// {Remote}{Local}[ItemID]
        /// </summary>
        public override string ToString() {
            if(Errored) return "";
            var ids = HasItemID ? $"{ItemID}" : "";
            return $"{(char)Remote}{(char)Local}{ids}";
        }


        /// <summary>
        /// Returns true if the SSM matches the passed StationStore
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMatch(in StationStore store) {
            return CheckCode(Local, store.localLogic)
                && CheckCode(Remote, store.remoteLogic)
                && (!HasItemID || store.itemId == ItemID)
            ;
        }

    }

    public class StationStorageMatchSet {
        private readonly StationStorageMatch[] _itemless;
        private readonly Dictionary<int, StationStorageMatch[]> _item;
        public bool HasMatchers => _itemless.Length != 0 && _item != null;

        public StationStorageMatchSet(string s) {
            if(!string.IsNullOrWhiteSpace(s)) {
                var raw = RythmnKit.ConfigSplit(s)
                    .Select(StationStorageMatch.FromString)
                    .Where(ssm => !ssm.Errored)
                    .ToList()
                ;
                _itemless = raw.Where(ssm => !ssm.HasItemID).ToArray();
                _item = raw.Where(ssm => ssm.HasItemID)
                    .GroupBy(ssm => ssm.ItemID)
                    .ToDictionary(grp => grp.Key, grp => grp.ToArray())
                ;
                if(_item.Count == 0) _item = null;
            } else {
                _itemless = Array.Empty<StationStorageMatch>();
                _item = null;
            }
        }

        public static StationStorageMatchSet FromString(string s) => new StationStorageMatchSet(s);
        public override string ToString() {
            if(!HasMatchers) return "";
            return RythmnKit.ConfigJoin(_item == null ? _itemless : _itemless.Concat(_item.Values.SelectMany(ssm => ssm)));
        }

        private bool HasItemlessMatch(in StationStore store) {
            if(_itemless.Length == 0) return true;
            for(int i = 0; i < _itemless.Length; i++) {
                if(_itemless[i].IsMatch(store)) {
                    return true;
                }
            }
            return false;
        }
        private bool HasItemMatch(in StationStore store) {
            if(_item == null
                || !_item.TryGetValue(store.itemId, out var tocheck)
                || tocheck.Length == 0
            ) {
                return true;
            }
            for(int i = 0; i < tocheck.Length; i++) {
                if(tocheck[i].IsMatch(store)) return true;
            }
            return false;
        }
        public bool IsMatch(in StationStore store) => HasItemlessMatch(store) && HasItemMatch(store);
    }
}
