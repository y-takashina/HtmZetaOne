using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmZetaOne;

namespace HtmZetaOneDemos
{
    public static class AnomalyDetection
    {
        private const int NumberSpatialPattern = 16;
        private const int NumberTemporalGroup = 8;

        [STAThread]
        public static void Main()
        {
            var rawStreams = new List<List<double>>();
            using (var sr = new StreamReader(@"..\..\data\water-treatment.csv"))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine().Split(',').Skip(1).ToArray();
                    if (!rawStreams.Any()) rawStreams.AddRange(line.Select(_ => new List<double>()));
                    for (var i = 0; i < line.Length; i++)
                    {
                        rawStreams[i].Add(line[i] == "?" ? double.NaN : double.Parse(line[i]));
                    }
                }
            }

            var level1 = rawStreams.Select(stream => new LeafNodeForContinuous(stream, stream, NumberSpatialPattern, NumberTemporalGroup));
            var level2 = Enumerable.Range(0, 6).Select(i => new InternalNode(level1.Where((v, j) => j % 6 == i), NumberTemporalGroup));
            var level3 = new InternalNode(level2, NumberTemporalGroup, Metrics.Shortest);
            level3.Learn();

            var anomalies = new[] {10, 11, 12, 78, 148, 186, 209, 292, 395, 398, 401, 441, 442, 443};
            var streamsByCluster = Enumerable.Range(0, NumberTemporalGroup)
                .Select(k => level3.ClusterStream
                    .Select((c, i) => (c, i))
                    .Where(t => t.Item1 == k)
                    .Select(t => t.Item2))
                .OrderByDescending(stream => stream.Count());
            for (var i = 0; i < NumberTemporalGroup + 1; i++)
            {
                var (precision, recall) = Utils.CalcPrecisionRecall(streamsByCluster.Skip(i).SelectMany(v => v), anomalies);
                var f = Utils.HarmonicMean(precision, recall);
                Console.WriteLine($"Precision: {precision,-6:f4}, Recall: {recall,-6:f4}, FMeasure: {f}");
            }

            Console.ReadLine();
        }
    }
}