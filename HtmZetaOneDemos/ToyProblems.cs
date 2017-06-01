using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.MachineLearning.Bayes;
using Accord.Math;
using HtmZetaOne;

namespace HtmZetaOneDemos
{
    class ToyProblems
    {
        public static void Main(string[] args)
        {
            var streams = new List<int[]>
            {
                new[] {0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 0},
                new[] {0, 1, 0, 1, 2, 3, 2, 3, 2, 3, 0, 1, 0, 0},
                new[] {0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0},
                new[] {5, 4, 5, 4, 2, 2, 2, 2, 2, 5, 4, 5, 4, 5}
            };
            var level1 = streams.Select(stream => new LeafNode(stream.Take(10), stream.Skip(10), 2));
            var level2Left = new InternalNode(level1.Take(2), 2); // Take the first two streams.
            var level2Right = new InternalNode(level1.Skip(2), 2); // Take the last two streams.
            var root = new InternalNode(new[] {level2Left, level2Right}, 2);

            // feedforward (clustering)
            root.Learn();
            foreach (var value in root.ClusterStream)
                Console.Write($"{value}, "); // output: 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1

            // feedback (classification)
            var labelStream = new[] {1, 1, 1, 0, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1};
            var labelNode = new LeafNode(labelStream.Take(10), Enumerable.Repeat(-1, 4), 2); // Use {-1} stream if want to generate 
            var superRoot = new InternalNode(new Node[] {root, labelNode}, 2);
            superRoot.Learn();

            while (superRoot.CanPredict)
            {
                var predicted = superRoot.Predict();
                superRoot.Generate(predicted);
            }
            var accuracy = (double) labelNode.GeneratedStream.Zip(labelStream.Skip(10), (predicted, actual) => predicted == actual ? 1 : 0).Sum();
            accuracy /= labelNode.GeneratedStream.Count;
            Console.WriteLine();
            Console.WriteLine($"accuracy: {accuracy}");
        }
    }
}