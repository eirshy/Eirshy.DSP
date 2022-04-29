using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace Eirshy.DSP.Rythmn {
    /// <summary>
    /// Various Helper Methods
    /// </summary>
    public static class UtilAndExt {

        public static void NoOp() { }
        public static void NoOp<T>(T _) { }

        #region Reflection Helpers

        /// <summary>
        /// Generates an instance of the attribute described by CustomAttributeData.
        /// </summary>
        [DebuggerStepThrough]
        public static Attribute ToInstance(this CustomAttributeData cad) {
            //Why is this part so convoluted? Because AttrData doesn't have a "gimme attr" function.
            var atr = (Attribute)cad.Constructor.Invoke(
                cad.ConstructorArguments.Select(cata => cata.Value).ToArray()
            );
            foreach(var named in cad.NamedArguments) {
                //could cache the props, but not sure if that's really necessary given this is a runonce function.
                var nprop = atr.GetType().GetProperty(named.MemberName, named.TypedValue.ArgumentType);
                nprop.SetValue(atr, named.TypedValue.Value);
            }
            return atr;
        }
        /// <summary>
        /// Generates a typed instance of the attribute described by CustomAttributeData.
        /// </summary>
        [DebuggerStepThrough]
        public static TAttribute ToInstance<TAttribute>(this CustomAttributeData cad) where TAttribute : Attribute {
            return (TAttribute)ToInstance(cad);
        }

        #endregion
        #region Property Expression Helpers

        /// <summary>
        /// Attempts to extract a PropertyInfo from the passed Expression. Returns null on failure.
        /// </summary>
        public static PropertyInfo ToPropInfo<TIn, TOut>(this Expression<Func<TIn, TOut>> expr) {
            return (expr.Body as MemberExpression).Member as PropertyInfo;
        }

        #endregion
        #region LINQ-style Extensions

        /// <summary>
        /// Evaluates the expression, calling Action on each item in this IEnumerable.
        /// </summary>
        /// <param name="action">Takes an item from the sequence.</param>
        public static void DoForEach<T>(this IEnumerable<T> iet, Action<T> action) {
            foreach(var t in iet) action(t);
        }
        /// <summary>
        /// Evaluates the expression, calling Action on each item in this IEnumerable.
        /// </summary>
        /// <param name="action">
        /// Takes an item from the sequence and the current execution's iteration number.
        /// <br />This number (should) match the index that .ToList() would have assigned the item.
        /// <br />NOT THREAD SAFE! You should use .ToList() if this number is going to be used for indexing
        /// into other sequences!
        /// </param>
        public static void DoForEach<T>(this IEnumerable<T> iet, Action<T, int> action) {
            var i = 0;
            foreach(var t in iet) action(t, i++);
        }
        /// <summary>
        /// Calls Action on each item in the list, additionally passing the index of the item.
        /// </summary>
        /// <param name="action"> Takes an item from the sequence and its index number. </param>
        public static void DoForEach<T>(this IList<T> ilt, Action<T, int> action) {
            for(int i = 0; i < ilt.Count; i++) {
                action(ilt[i], i);
            }
        }
        /// <summary>
        /// Evaluates the expression, calling Action on each item in this IEnumerable.
        /// </summary>
        /// <param name="action">Takes an item from the sequence.</param>
        /// <param name="chain">The value you want us to return for chaining purposes</param>
        public static TC DoForEach<T,TC>(this IEnumerable<T> iet, Action<T> action, TC chain) {
            foreach(var t in iet) action(t);
            return chain;
        }
        /// <summary>
        /// Evaluates the expression, calling Action on each item in this IEnumerable.
        /// </summary>
        /// <param name="action">
        /// Takes an item from the sequence and the current execution's iteration number.
        /// <br />This number (should) match the index that .ToList() would have assigned the item.
        /// <br />NOT THREAD SAFE! You should use .ToList() if this number is going to be used for indexing
        /// into other sequences!
        /// </param>
        /// <param name="chain">The value you want us to return for chaining purposes</param>
        public static TC DoForEach<T, TC>(this IEnumerable<T> iet, Action<T, int> action, TC chain) {
            var i = 0;
            foreach(var t in iet) action(t, i++);
            return chain;
        }
        /// <summary>
        /// Calls Action on each item in the list, additionally passing the index of the item.
        /// </summary>
        /// <param name="action"> Takes an item from the sequence and its index number. </param>
        /// <param name="chain">The value you want us to return for chaining purposes</param>
        public static TC DoForEach<T, TC>(this IList<T> ilt, Action<T, int> action, TC chain) {
            for(int i = 0; i < ilt.Count; i++) {
                action(ilt[i], i);
            }
            return chain;
        }

        /// <summary>
        /// Returns an IEnumerable containing only this element, via <c>yield return item</c>
        /// </summary>
        /// <remarks>
        /// While simply <c>yield return item;</c> is "cleaner", it's actually generally more performant
        /// to actually pay for the single-item array alloc since the compiler has a ton of opts it can
        /// do for arrays, and single item arrays are ultra thin.
        /// <br />
        /// src (with benchmarks from 2020): https://stackoverflow.com/questions/62296111/
        /// <br />
        /// Technically, a <c>params</c>-based alloc would be even better, but that can't be an extension method.
        /// --mcg
        /// </remarks>
        public static IEnumerable<T> ToEnumerable<T>(this T item) => new[] { item };
        /// <summary>
        /// Returns a HashSet sized exactly to the contents of this enumerable.
        /// 
        /// Totally not here because for some reason ToHashSet sometimes doesn't exist.
        /// </summary>
        public static HashSet<T> ToSizedHashSet<T>(this IEnumerable<T> iet) {
            var asl = iet as List<T> ?? iet.ToList();
            var ret = new HashSet<T>(asl.Count);
            foreach(var t in asl) ret.Add(t);
            return ret;
        }
        /// <summary>
        /// Returns a HashSet sized exactly to the contents of this enumerable, containing the property from selector.
        /// </summary>
        public static HashSet<TOut> ToSelectedHashSet<TIn, TOut>(this IEnumerable<TIn> iet, Expression<Func<TIn, TOut>> selector) {
            var sel = selector.Compile();
            var asl = iet as List<TIn> ?? iet.ToList();
            var ret = new HashSet<TOut>(asl.Count);
            foreach(var t in asl) ret.Add(sel(t));
            return ret;
        }

        /// <summary>
        /// Adds every param to this collection.
        /// </summary>
        public static void Add<T>(this ICollection<T> ilt, params T[] toAdd) {
            foreach(var t in toAdd) ilt.Add(t);
        }

        #endregion

        public sealed class TypeComparer : IComparer<Type> {
            public static TypeComparer Instance = new TypeComparer();
            public int Compare(Type x, Type y) => x?.Name?.CompareTo(y?.Name) ?? (x is null && y is null ? 0 : -1);
        }
    }
}
