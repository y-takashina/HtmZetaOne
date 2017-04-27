using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Accord.Statistics;
using Accord.Math;

namespace HtmZetaOne
{
    public class LeafNode : Node
    {
        private readonly double _deviation;
        private readonly List<double> _means;
        private IEnumerable<double> _testStream;
        public override int N => _means?.Count ?? 0;
        public override bool CanPredict => _testStream?.Any() ?? false;

        public LeafNode(IEnumerable<double> trainStream, IEnumerable<double> testStream, int numberSpatialPattern, int numberTemporalGroup, Func<(double, int), (double, int), double> metrics = null) : base(numberTemporalGroup, metrics)
        {
            _deviation = trainStream.Where(v => !double.IsNaN(v)).ToArray().Variance();
            _means = Sampling.QuantizeByKMeans(trainStream.Where(v => !double.IsNaN(v)).ToArray(), numberSpatialPattern).ToList();
            Stream = trainStream.Select(v => double.IsNaN(v) ? new Random().Next(N) : _means.IndexOf(_means.MinBy(m => Math.Abs(m - v))));
            _testStream = testStream;
        }

        public override double[] Predict()
        {
            if (!CanPredict) throw new NullReferenceException("Cannot predict anything. _testStream is null or empty.");
            var value = _testStream.First();
            _testStream = _testStream.Skip(1);
            var coincidence = new double[_means.Count];
            if (double.IsNaN(value))
            {
                coincidence = Enumerable.Repeat(1.0 / _means.Count, _means.Count).ToArray();
            }
            else
            {
                for (var i = 0; i < _means.Count; i++)
                {
                    var d = _means[i] - value;
                    coincidence[i] = Math.Exp(-d * d / _deviation);
                }
            }
            return Forward(coincidence);
        }
    }

    public class InternalNode : Node
    {
        private readonly IEnumerable<Node> _childNodes;
        public List<int[]> SpatialPooler { get; set; }
        public override int N => SpatialPooler?.Count ?? 0;
        public override bool CanPredict => _childNodes.Aggregate(true, (can, child) => can && child.CanPredict);

        public InternalNode(IEnumerable<Node> childNodes, int numberTemporalGroup, Func<(double, int), (double, int), double> metrics = null) : base(numberTemporalGroup, metrics)
        {
            if (childNodes == null) throw new NullReferenceException("`childNodes` is null.");
            if (childNodes.Contains(null)) throw new NullReferenceException("`childNodes` contains null.");
            _childNodes = childNodes;
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

        public void Memorize(IEnumerable<int[]> rawStream)
        {
            SpatialPooler = new List<int[]>();
            foreach (var value in rawStream)
            {
                var memorized = SpatialPooler.Any(memoizedValue => memoizedValue.SequenceEqual(value));
                if (!memorized) SpatialPooler.Add(value);
            }
            Stream = rawStream.Select(value => SpatialPooler.IndexOf<int[]>(value));
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

        /// <summary>
        /// number of coincidence
        /// </summary>
        public abstract int N { get; }

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
            var temporalGroup = Enumerable.Range(0, M).Select(i => input.Select((v, j) => v * Membership[j, i]).Max()).ToArray().Normalize();
            return temporalGroup;
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