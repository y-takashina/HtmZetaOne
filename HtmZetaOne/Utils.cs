using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

namespace HtmZetaOne
{
    public static class Utils
    {
        public static int IndexOf<T>(this IEnumerable<T> source, T value) where T : IEnumerable<int>
        {
            var array = source.ToArray();
            for (var i = 0; i < array.Length; i++) if (array[i].SequenceEqual(value)) return i;
            return -1;
        }

        public static double[,] NormalizeToRaws(this double[,] a, double tolerance = 1e-6)
        {
            var raws = a.GetLength(0);
            var cols = a.GetLength(1);
            var b = new double[raws, cols];
            for (var i = 0; i < raws; i++)
            {
                var sum = 0.0;
                for (var j = 0; j < cols; j++)
                {
                    sum += a[i, j];
                }
                for (var j = 0; j < cols; j++)
                {
                    b[i, j] = Math.Abs(sum) < tolerance ? 1.0 / raws : a[i, j] / sum;
                }
            }
            return b;
        }

        public static (double precision, double recall) CalcPrecisionRecall(IEnumerable<int> predicted, IEnumerable<int> actual)
        {
            var nTruePositives = predicted.Intersect(actual).Count();
            var nPredictedPositives = predicted.Count();
            var nActualPositives = actual.Count();
            return ((double) nTruePositives / nPredictedPositives, (double) nTruePositives / nActualPositives);
        }

        public static double HarmonicMean(double v1, double v2)
        {
            return 2 * v1 * v2 / (v1 + v2);
        }
    }
}