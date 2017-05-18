using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using HtmZetaOne;
using static HtmZetaOneDemos.Mnist;

namespace HtmZetaOneDemos
{
    class DigitClassification
    {
        public static void Main(string[] args)
        {
            var root = Path.Combine("..", "..", "data", "mnist");
            var pathImage = Path.Combine(root, "train-images-idx3-ubyte");
            var pathLabel = Path.Combine(root, "train-labels-idx1-ubyte");
            var digits = LoadData(pathImage, pathLabel); // digits[0] = 5, digits[1] = 0, digits[11] = 5
            var digits05 = digits.Where(digit => digit.Label == 0 | digit.Label == 5).ToArray();

            // generate data
            var screen = new Screen(28, 28);
            var movie = new List<byte[,]>();
            foreach (var digit in digits.Take(2))
            {
                for (var x = -28; x < 28; x++)
                {
                    for (var y = -28; y < 28; y++)
                    {
                        if (x % 2 == 0) screen.Locate(digit, x, y);
                        else screen.Locate(digit, x, -y);
                        movie.Add(screen.Pixels.DeepClone());
                    }
                }
            }
            var streams = new List<int>[27, 27];
            for (var i = 0; i < 27; i++)
            {
                for (var j = 0; j < 27; j++)
                {
                    streams[i, j] = new List<int>();
                    foreach (var frame in movie)
                    {
                        streams[i, j].Add(frame[i, j]);
                    }
                }
            }
            var labelStream = new List<int>();
            foreach (var digit in digits.Take(2))
            {
                labelStream.AddRange(Enumerable.Repeat((int) digit.Label, 56 * 56));
            }
            var testStreams = new List<int>[27, 27];
            for (var i = 0; i < 27; i++)
            {
                for (var j = 0; j < 27; j++)
                {
                    testStreams[i, j] = new List<int>(); // label: 5
                    foreach (var digit in digits05)
                    {
                        testStreams[i, j].Add(digit.Pixels[i, j]);
                    }
                }
            }
            var testLabelStream = new List<int>();
            foreach (var digit in digits05)
            {
                testLabelStream.Add(digit.Label);
            }
            Console.WriteLine("Data generation finished.");

            // build network
            var level1 = new LeafNode[27, 27];
            Parallel.For(0, 27, y =>
            {
                for (var x = 0; x < 27; x++)
                {
                    level1[y, x] = new LeafNode(streams[y, x], testStreams[y, x], 2, Metrics.GroupAverage);
                }
            });
            Console.WriteLine("level1 finished");
            var level2 = new InternalNode[9, 9];
            for (var y = 0; y < 9; y++)
            {
                for (var x = 0; x < 9; x++)
                {
                    var childNodes = new List<Node>();
                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            childNodes.Add(level1[y * 3 + i, x * 3 + j]);
                        }
                    }
                    level2[y, x] = new InternalNode(childNodes, 4, Metrics.GroupAverage);
                }
            }
            Console.WriteLine("level2 finished");
            var level3 = new InternalNode[3, 3];
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 3; x++)
                {
                    var childNodes = new List<Node>();
                    for (var i = 0; i < 3; i++)
                    {
                        for (var j = 0; j < 3; j++)
                        {
                            childNodes.Add(level1[y * 3 + i, x * 3 + j]);
                        }
                    }
                    level3[y, x] = new InternalNode(childNodes, 4, Metrics.GroupAverage);
                }
            }
            Console.WriteLine("level3 finished");
            var labelNode = new LeafNode(labelStream, Enumerable.Repeat(-1, 56 * 56 * 2), 2);
            var children = new List<Node>();
            children.AddRange(level3.Cast<Node>());
            children.Add(labelNode);
            var level4 = new InternalNode(children, 2);
            Console.WriteLine("Network building finished.");

            // learn
            level4.Learn();
            Console.WriteLine("Learning finished.");

            Console.WriteLine(level4.M);
            Console.WriteLine(level4.N);
//            while (level4.CanPredict)
            for (var i = 0; i < 2; i++)
            {
                var predicted = level4.Predict();
                var argmax = predicted.ArgMax();
                level4.Generate(argmax);
            }
            var accuracy = 0.0;
            for (var i = 0; i < 2; i++)
            {
                if (labelNode.GeneratedStream[i] == testLabelStream[i]) accuracy++;
            }
            accuracy /= 2;
            Console.WriteLine($"accuracy: {accuracy}");

            Console.ReadLine();

//            for (var i = 200; i < 300; i++)
//            {
//                movie[i].ToBitmap(4).Save($@"output\{i}.png", ImageFormat.Png);
//            }
//            for (var i = 0; i < 100; i++)
//            {
//                screen.Locate(i - 28, 23, digits[0]);
//                screen.Save($@"output\{i}.png", ImageFormat.Png, 5);
//            }
        }
    }
}