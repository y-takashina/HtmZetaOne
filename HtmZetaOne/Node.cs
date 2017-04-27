using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Accord.Statistics;
using Accord.Math;

namespace HtmZetaOne
{
    /// <summary>
    /// Level 1 node in HTM. Only available with discrete values.
    /// If you want to deal with more complex data (e.g. images),
    /// you must discretize your data and feed its indices to the LeafNode.
    /// </summary>
    public sealed class LeafNode : Node
    {
        private IEnumerable<int> _testStream;
        public override bool CanPredict => _testStream?.Any() ?? false;

        public LeafNode(IEnumerable<int> trainStream, IEnumerable<int> testStream, int numberTemporalGroup, Func<(double, int), (double, int), double> metrics = null) : base(numberTemporalGroup, metrics)
        {
            Memorize(trainStream.Select(v => new[] {v}));
            _testStream = testStream;
        }

        public override double[] Predict()
        {
            if (!CanPredict) throw new NullReferenceException("Cannot predict anything. _testStream is null or empty.");
            var value = _testStream.First();
            _testStream = _testStream.Skip(1);
            var coincidence = new double[N];
            if (value == -1)
            {
                coincidence = Enumerable.Repeat(1.0 / N, N).ToArray();
            }
            else
            {
                var index = SpatialPooler.IndexOf<int[]>(new[] {value});
                coincidence[index] = 1.0;
            }
            return Forward(coincidence);
        }
    }

    /// <summary>
    /// Level 1 node in HTM. Available with continuous values.
    /// Only single dimension is allowd.
    /// </summary>
    public sealed class LeafNodeForContinuous : Node
    {
        private readonly double _deviation;
        private readonly List<double> _means;
        private IEnumerable<double> _testStream;
        public override int N => _means?.Count ?? 0;
        public override bool CanPredict => _testStream?.Any() ?? false;

        public LeafNodeForContinuous(IEnumerable<double> trainStream, IEnumerable<double> testStream, int numberSpatialPattern, int numberTemporalGroup, Func<(double, int), (double, int), double> metrics = null) : base(numberTemporalGroup, metrics)
        {
            // memorize means instead of SpatialPooler
            _means = Quantization.QuantizeByKMeans(trainStream.Where(v => !double.IsNaN(v)).ToArray(), numberSpatialPattern).ToList();
            _deviation = trainStream.Where(v => !double.IsNaN(v)).ToArray().Variance();
            var rand = new Random();
            Stream = trainStream.Select(v => double.IsNaN(v) ? rand.Next(N) : _means.IndexOf(_means.MinBy(m => Math.Abs(m - v))));
            _testStream = testStream;
        }

        public override double[] Predict()
        {
            if (!CanPredict) throw new NullReferenceException("Cannot predict anything. _testStream is null or empty.");
            var value = _testStream.First();
            _testStream = _testStream.Skip(1);
            var coincidence = new double[N];
            if (double.IsNaN(value))
            {
                coincidence = Enumerable.Repeat(1.0 / N, N).ToArray();
            }
            else
            {
                for (var i = 0; i < N; i++)
                {
                    var d = _means[i] - value;
                    coincidence[i] = Math.Exp(-d * d / _deviation);
                }
            }
            return Forward(coincidence);
        }
    }

    /// <summary>
    /// Level 2 or higher node in HTM.
    /// </summary>
    public class InternalNode : Node
    {
        private readonly IEnumerable<Node> _childNodes;
        public override bool CanPredict => _childNodes.Aggregate(true, (can, child) => can && child.CanPredict);

        public InternalNode(IEnumerable<Node> childNodes, int numberTemporalGroup, Func<(double, int), (double, int), double> metrics = null) : base(numberTemporalGroup, metrics)
        {
            if (childNodes == null) throw new NullReferenceException("`childNodes` is null.");
            if (childNodes.Contains(null)) throw new NullReferenceException("`childNodes` contains null.");
            _childNodes = childNodes.ToArray();
        }

        private IEnumerable<int[]> _aggregateChildStreams()
        {
            var childStreams = _childNodes.Select(node => node.Stream.Select(node.Forward).ToArray()).ToArray();
            var rawStream = childStreams.First().Select(_ => new List<int>()).ToList();
            foreach (var childStream in childStreams)
            {
                for (var i = 0; i < childStream.Length; i++)
                {
                    rawStream[i].Add(childStream[i]);
                }
            }
            return rawStream.Select(coincidence => coincidence.ToArray());
        }

        public override void Learn()
        {
            foreach (var childNode in _childNodes) childNode.Learn();
            Memorize(_aggregateChildStreams());
            base.Learn();
        }

        public override double[] Predict()
        {
            var childOutputs = _childNodes.Select(node => node.Predict()).ToArray();
            var coincidence = Enumerable.Repeat(1.0, N).ToArray();
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < SpatialPooler[i].Length; j++)
                {
                    coincidence[i] *= childOutputs[j][SpatialPooler[i][j]];
                }
            }
            return Forward(coincidence);
        }
    }

    public abstract class Node
    {
        private readonly Func<(double, int), (double, int), double> _metrics;
        public List<int[]> SpatialPooler { get; set; }

        /// <summary>
        /// number of coincidence
        /// </summary>
        public virtual int N => SpatialPooler?.Count ?? 0;

        /// <summary>
        /// number of temporal group
        /// </summary>
        public int M { get; set; }

        public abstract bool CanPredict { get; }
        public IEnumerable<int> Stream { get; set; }
        public IEnumerable<int> ClusterStream => Stream.Select(Forward);
        public int[,] Membership { get; set; }

        protected Node(int numberTemporalGroup, Func<(double, int), (double, int), double> metrics)
        {
            _metrics = metrics ?? Metrics.GroupAverage;
            M = numberTemporalGroup;
        }

        /// <summary>
        /// hard forward
        /// </summary>
        public int Forward(int input)
        {
            if (Membership == null) throw new NullReferenceException("Membership is null. Learning has not been completed properly.");
            for (var i = 0; i < M; i++) if (Membership[input, i] == 1) return i;
            throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// soft forward
        /// </summary>
        public double[] Forward(double[] input)
        {
            if (input.Length != N) throw new IndexOutOfRangeException("Feedforward input to a node must have the same length as the node's spatial pooler.");
            var temporalGroup = Enumerable.Range(0, M).Select(i => input.Select((v, j) => v * Membership[j, i]).Max());
            var sum = temporalGroup.Sum();
            return temporalGroup.Select(v => v / sum).ToArray();
        }

        public int[] Backward(int input)
        {
            var coincidence = new int[N];
            for (var i = 0; i < N; i++) coincidence[i] = Membership[i, input];
            return coincidence;
        }

        public double[] Backward(double[] input)
        {
            if (input.Length != M) throw new IndexOutOfRangeException("Feedback input to a node must have the same length as the node's temporal pooler.");
            throw new NotImplementedException();
        }

        protected virtual void Memorize(IEnumerable<int[]> rawStream)
        {
            SpatialPooler = new List<int[]>();
            foreach (var value in rawStream)
            {
                var memorized = SpatialPooler.Any(memoizedValue => memoizedValue.SequenceEqual(value));
                if (!memorized) SpatialPooler.Add(value);
            }
            Stream = rawStream.Select(value => SpatialPooler.IndexOf<int[]>(value));
        }

        public virtual void Learn()
        {
            var transitions = new double[N, N];
            var stream = Stream.ToArray();
            for (var i = 0; i < stream.Length - 1; i++)
            {
                transitions[stream[i], stream[i + 1]]++;
            }
            var probabilities = transitions.NormalizeToRaws();
            var distances = probabilities.Add(probabilities.Transpose()).Multiply(-1);
            var cluster = Clustering.AggregativeHierarchicalClustering(Enumerable.Range(0, N), (i, j) => distances[i, j], _metrics);
            var clusterwiseMembers = cluster.Extract(M).Select(c => c.SelectMany(v => v)).ToArray();
            Membership = new int[N, M];
            for (var i = 0; i < N; i++)
            {
                for (var j = 0; j < M; j++)
                {
                    Membership[i, j] = clusterwiseMembers[j].Contains(i) ? 1 : 0;
                }
            }
        }

        public abstract double[] Predict();
    }
}