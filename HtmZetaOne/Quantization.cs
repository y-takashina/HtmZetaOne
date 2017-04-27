using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Accord.Statistics;

namespace HtmZetaOne
{
    public static class Quantization
    {
        public static double[] QuantizeAtRegularIntervals(IEnumerable<double> data, int n, bool leaveOutlier = true)
        {
            var array = data.Where(v => !double.IsNaN(v)).ToArray();
            var average = array.Mean();
            var stddev = array.StandardDeviation();
            var min = average - 3 * stddev;
            var max = average + 3 * stddev;
            var interval = (max - min) / n;
            var points = Enumerable.Range(0, n).Select(i => max - i * interval).ToArray();
            if (leaveOutlier)
            {
                points[0] = average + 4 * stddev;
                points[n - 1] = average - 4 * stddev;
            }
            return points;
        }

        public static double[] QuantizeByKMeans(IEnumerable<double> data, int k)
        {
            var model = new KMeans(k) {UseSeeding = Seeding.Uniform};
            model.Learn(data.Select(v => new[] {v}).ToArray());
            return model.Clusters.Centroids.Select(vector => vector.First()).ToArray();
        }
    }
}