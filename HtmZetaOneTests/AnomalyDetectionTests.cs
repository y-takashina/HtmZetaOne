using Microsoft.VisualStudio.TestTools.UnitTesting;
using HtmZetaOne;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreLinq;

namespace HtmZetaOneTests
{
    [TestClass()]
    public class AnomalyDetectionTests
    {
        private readonly Node _root;
        private const int NumberSpatialPattern = 16;
        private const int NumberTemporalGroup = 8;

        public AnomalyDetectionTests()
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

            /*
            var level1 = rawStreams.Select(stream => new LeafNode(stream, stream, NumberSpatialPattern, NumberTemporalGroup));
            var level2 = Enumerable.Range(0, 6).Select(i => new InternalNode(level1.Where((v, j) => j % 6 == i).ToArray(), NumberTemporalGroup));
            _root = new InternalNode(level2.ToArray(), NumberTemporalGroup, Metrics.Shortest);
            _root.Learn();
            //*/
        }

        [TestMethod()]
        public void AnomalyDetectionTest()
        {
            var streamsByCluster = Enumerable.Range(0, NumberTemporalGroup)
                .Select(k => _root.ClusterStream
                    .Select((c, i) => (c, i))
                    .Where(t => t.Item1 == k)
                    .Select(t => t.Item2))
                .OrderByDescending(stream => stream.Count());
            for (var i = 0; i < NumberTemporalGroup + 1; i++)
            {
                var pr = CalcPr(streamsByCluster.Skip(i));
                var f = 2 * pr.Item1 * pr.Item2 / (pr.Item1 + pr.Item2);
                Console.WriteLine($"Precision: {pr.Item1,-6:f4}, Recall: {pr.Item2,-6:f4}, FMeasure: {f}");
            }
        }

        (Cluster<int> cluster, Node node) AggregateClusters(Cluster<int> cluster, LeafNode[] leafNodes, int nTemporalGroup)
        {
            if (cluster is Single<int> single) return (null, leafNodes[single.Value]);
            var couple = (Couple<int>) cluster;
            var left = AggregateClusters(couple.Left, leafNodes, nTemporalGroup).node;
            var right = AggregateClusters(couple.Right, leafNodes, nTemporalGroup).node;
            return (null, new InternalNode(new[] {left, right}, nTemporalGroup, Metrics.GroupAverage));
        }

        (double, double) CalcPr(IEnumerable<IEnumerable<int>> streams)
        {
            var stream = streams.SelectMany(v => v);
            var anomalies = new[] {10, 11, 12, 78, 148, 186, 209, 292, 395, 398, 401, 441, 442, 443};
            var nTp = stream.Intersect(anomalies).Count();
            return ((double) nTp / stream.Count(), (double) nTp / anomalies.Length);
        }
    }
}