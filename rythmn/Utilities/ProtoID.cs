using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;


namespace Eirshy.DSP.Rythmn.Utilities {
    public abstract class ProtoID {
        public abstract int Id { get; }
        protected string AsString { get; set; }

        public string GetName() {
            _GenerateName();
            return AsString;
        }
        public string GetFullName() => GetType().Name + "." + GetName();

        /// <summary>
        /// Generates this entity's name if it hasn't been generated yet.
        /// </summary>
        protected abstract void _GenerateName();

        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString() => GetFullName();
        

        public ItemProto Proto => LDB.items.Select(Id);

    }

    /// <summary>
    /// A fancy pseudo-enum wrapper for proto IDs
    /// </summary>
    /// <remarks>
    /// Uses Reflection to get all public-static TImplementor values on TImplementor,
    /// and generate the following helpers:
    /// <br />&gt; implicit casts to and from integers via our "registered" integer value
    /// <br />&gt; IComparable to our same entity type via our "registered" integer value
    /// <br />&gt; Setting the Name properties of our enumeration, via our "registered" string value
    /// <br />&gt; methods _AsDictionary, _GetAll, _TryGetValue, _GenerateNames
    /// </remarks>
    /// <typeparam name="TImplementor">
    /// The implementing type.
    /// <br />
    /// A more constrained form of the Curiously Recurring Template Pattern,
    /// but only as enforced as the CRTP is because C# 7 has no way to enforce
    /// the constraint as specifically as would be ideal.
    /// </typeparam>
    public abstract class ProtoID<TImplementor> : ProtoID
        , IComparable<TImplementor>, IComparable<ProtoID<TImplementor>>
        , IEquatable<TImplementor>, IEquatable<ProtoID<TImplementor>>
        where TImplementor : ProtoID<TImplementor>
    {
        public IEnumerable<TImplementor> E() => new[] { (TImplementor)this };
        public IEnumerable<TImplementor> E(params TImplementor[] args) => args.Prepend((TImplementor)this);

        #region Reflection Magic and comfyifying stuff

        /// <summary>
        /// <c>(RM)</c>Source; Reflection magic to get all possible values of our "enum" as a dictionary
        /// </summary>
        private readonly static Lazy<IDictionary<int, TImplementor>> __all = new Lazy<IDictionary<int, TImplementor>>(()=>{
            #region ... ... ... ... ...
            var props = typeof(TImplementor).GetProperties(
                BindingFlags.Public | BindingFlags.Static
                | BindingFlags.DeclaredOnly //only the ones TImplementor's decl itself declares
            );
            //get our values
            var values = new List<TImplementor>(props.Length);
            foreach(var prop in props){
                if(prop.PropertyType == typeof(TImplementor)){
                    var value = (TImplementor)prop.GetMethod.Invoke(null, null);
                    value.AsString = prop.Name;
                    values.Add(value);
                }
            }

            //make the dictionary the exact right size and populate it.
            var ret = new Dictionary<int, TImplementor>(values.Count);
            foreach(var value in values) ret.Add(value.Id, value);
            return ret;

            #endregion
        });


        /// <summary>
        /// Helper<c>(RM)</c>; Provides a full Dictionary reference for this "enum"
        /// </summary>
        public static IReadOnlyDictionary<int, TImplementor> _AsDictionary() {
            return (IReadOnlyDictionary<int, TImplementor>)__all.Value;
        }


        /// <summary>
        /// Helper<c>(RM)</c>; Provides a collection of all values in this "enum"
        /// </summary>
        public static ICollection<TImplementor> _GetAll() {
            return __all.Value.Values;
        }


        /// <summary>
        /// Helper<c>(RM)</c>; Attempts to get the enum value for the passed integer. Returns null on miss.
        /// </summary>
        public static TImplementor _TryGetValue(int i) {
            return __all.Value.TryGetValue(i, out var o) ? o : null;
        }
        /// <summary>
        /// Helper<c>(RM)</c>; Gets the value associated with the given int, ala Dictionary.TryGetValue
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public static bool _TryGetValue(int i, out TImplementor @out) {
            return __all.Value.TryGetValue(i, out @out);
        }

        /// <summary>
        /// Helper<c>(RM)</c>; Forces the internal reflection magic to run so the Name fields will be populated.
        /// <br />Not needed if you've previously called any of the other Helpers marked with <c>(RM)</c>.
        /// </summary>
        public static void _GenerateNames() => _ = __all.Value;
        protected sealed override void _GenerateName() => _GenerateNames();

        //implicit unwrap - implicit wrap is given by inheritence
        public static implicit operator TImplementor(ProtoID<TImplementor> self) => (TImplementor)self;

        //To/from integer
        /// <summary>
        /// from-int; throws InvalidCastException if the given integer cannot be converted
        /// </summary>
        /// <exception cref="InvalidCastException" />
        public static explicit operator ProtoID<TImplementor>(int i) {
            if (__all.Value.TryGetValue(i, out var o)) return o;
            else throw new InvalidCastException();
        }
        public static implicit operator int(ProtoID<TImplementor> ent) => ent.Id;

        //To/from nullable integer
        /// <summary>
        /// from-in?t; throws InvalidCastException if the given integer cannot be converted
        /// </summary>
        /// <exception cref="InvalidCastException" />
        public static explicit operator ProtoID<TImplementor>(int? ni) {
            if (ni.HasValue) {
                if (__all.Value.TryGetValue(ni.Value, out var o)) return o;
                else throw new InvalidCastException();
            } else return null;
        }
        public static implicit operator int?(ProtoID<TImplementor> ent) => ent?.Id;

        #region Object overrides
        
        public override sealed bool Equals(object o) {
            if (o is IEquatable<ProtoID<TImplementor>> tse) return tse.Equals(this);
            else if (o is IEquatable<TImplementor> ti) return ti.Equals((TImplementor)this);
            else if (o is int i) return Id == i;
            else return false;
        }
        public override sealed int GetHashCode() => Id.GetHashCode();

        #endregion
        #region IComparables -- implements both to minimize the amount of wrapper-adjustment necessary

        public int CompareTo(TImplementor other) => Id.CompareTo(other.Id);
        public int CompareTo(ProtoID<TImplementor> other) => Id.CompareTo(other.Id);

        #endregion
        #region IEquitable -- implements both to minimize the amount of wrapper-adjustment necessary
        
        public bool Equals(TImplementor other) => Id.Equals(other.Id);
        public bool Equals(ProtoID<TImplementor> other) => Id.Equals(other.Id);

        #endregion
        #region Implement operators through op(a,b)=>op(a?.AsInt, b?.AsInt)

        public static bool operator ==(ProtoID<TImplementor> a, ProtoID<TImplementor> b) {
            return a?.Id == b?.Id;
        }
        public static bool operator !=(ProtoID<TImplementor> a, ProtoID<TImplementor> b) {
            return a?.Id != b?.Id;
        }

        public static bool operator >(ProtoID<TImplementor> a, ProtoID<TImplementor> b) {
            return a?.Id > b?.Id;
        }
        public static bool operator >=(ProtoID<TImplementor> a, ProtoID<TImplementor> b) {
            return a?.Id >= b?.Id;
        }
        public static bool operator <(ProtoID<TImplementor> a, ProtoID<TImplementor> b) {
            return a?.Id < b?.Id;
        }
        public static bool operator <=(ProtoID<TImplementor> a, ProtoID<TImplementor> b) {
            return a?.Id <= b?.Id;
        }

        #endregion

        #endregion
    }

}
