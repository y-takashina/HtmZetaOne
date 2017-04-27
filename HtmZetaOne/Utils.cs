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

        public static IEnumerable<int> Discretize(this IEnumerable<double> rawStream, int k)
        {
            var discretizedValues = Sampling.QuantizeByKMeans(rawStream, k).ToList();
            var discretizedSeries = new List<int>();
            foreach (var value in rawStream)
            {
                var discretizedValue = double.IsNaN(value) ? value : discretizedValues.Where(v => !double.IsNaN(v)).MinBy(v => Math.Abs(v - value));
                var discretizedValueIndex = discretizedValues.IndexOf(discretizedValue);
                discretizedSeries.Add(discretizedValueIndex);
            }
            return discretizedSeries;
        }

        public static double Entropy(this IEnumerable<double> probabilities)
        {
            return probabilities.Where(p => Math.Abs(p) > 1e-300).Sum(p => -p * Math.Log(p, 2));
        }


        public static double Entropy<T>(this IEnumerable<T> stream)
        {
            var array = stream.ToArray();
            var points = array.Distinct().ToArray();
            var probabilities = points.Select(v1 => (double) array.Count(v2 => Equals(v1, v2)) / array.Length);
            return probabilities.Entropy();
        }

        public static double JointEntropy(IEnumerable<int> stream1, IEnumerable<int> stream2)
        {
            return stream1.Zip(stream2, Tuple.Create).Entropy();
        }

        public static double MutualInformation(IEnumerable<int> stream1, IEnumerable<int> stream2)
        {
            return stream1.Entropy() + stream2.Entropy() - JointEntropy(stream1, stream2);
        }

        public static double[,] MutualInformation(IEnumerable<IEnumerable<int>> streams)
        {
            var arrays = streams as IEnumerable<int>[] ?? streams.ToArray();
            var n = arrays.Length;
            var matrix = new double[n, n];
            for (var j = 0; j < n; j++)
            {
                for (var k = j; k < n; k++)
                {
                    matrix[j, k] = matrix[k, j] = MutualInformation(arrays[j], arrays[k]);
                }
            }
            return matrix;
        }
    }
}