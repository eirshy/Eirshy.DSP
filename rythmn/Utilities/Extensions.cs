using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eirshy.DSP.Rythmn.Utilities {
    public static class Extensions {
        public static int Between(this int i, int min, int max)
            => i > max ? max : i < min ? min : i;
        public static double Between(this double d, double min, double max)
            => d > max ? max : d < min ? min : d;

        public static int ToIntTrunc(this double dbl) => (int)Math.Truncate(dbl);
        public static int ToIntFloor(this double dbl) => (int)Math.Floor(dbl);
        public static int ToIntCeil(this double dbl) => (int)Math.Ceiling(dbl);
        public static int ToIntRound(this double dbl) => (int)Math.Round(dbl);

        public static T Into<T>(this int i, List<T> list) => list[i.Between(0, list.Count -1)];
        public static T Into<T>(this int i, params T[] arr) => arr[i.Between(0, arr.Length - 1)];
        public static T IntoCyclic<T>(this int i, List<T> list) => list[i.Between(0, i) % list.Count];
        public static T IntoCyclic<T>(this int i, params T[] arr) => arr[i.Between(0, i) % arr.Length];
    }
}
