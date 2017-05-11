using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            var movie = new List<byte[,]>();
            var digits = LoadData(pathImage, pathLabel);

            // generate data
            var screen = new Screen(28, 28);
            for (var i = 0; i < 10; i++)
            {
                for (var x = -28; x < 28; x++)
                {
                    for (var y = -28; y < 28; y++)
                    {
                        if (x % 2 == 0) screen.Locate(digits[i], x, y);
                        else screen.Locate(digits[i], x, -y);
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
            for (var i = 0; i < 5; i++)
            {
                labelStream.AddRange(Enumerable.Repeat((int) digits[i].Label, 56 * 56));
            }
            Console.WriteLine("Data generation finished.");

            // build network
            var level1 = new LeafNode[27, 27];
            Parallel.For(0, 27, i =>
            {
                for (var j = 0; j < 27; j++)
                {
                    level1[i, j] = new LeafNode(streams[i, j], null, 2);
                }
            });
            Console.WriteLine("level1 finished");
            var level2 = new InternalNode[9, 9];
            for (var i = 0; i < 9; i++)
            {
                for (var j = 0; j < 9; j++)
                {
                    var childNodes = new List<Node>();
                    for (var ci = 0; ci < 3; ci++)
                    {
                        for (var cj = 0; cj < 3; cj++)
                        {
                            childNodes.Add(level1[i * 3 + ci, j * 3 + cj]);
                        }
                    }
                    level2[i, j] = new InternalNode(childNodes, 8);
                }
            }
            Console.WriteLine("level2 finished");
            var level3 = new InternalNode[3, 3];
            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var childNodes = new List<Node>();
                    for (var ci = 0; ci < 3; ci++)
                    {
                        for (var cj = 0; cj < 3; cj++)
                        {
                            childNodes.Add(level1[i * 3 + ci, j * 3 + cj]);
                        }
                    }
                    level3[i, j] = new InternalNode(childNodes, 8);
                }
            }
            Console.WriteLine("level3 finished");
            var labelNode = new LeafNode(labelStream, null, 5);
            var children = new List<Node>();
            children.AddRange(level3.Cast<Node>());
            children.Add(labelNode);
            var level4 = new InternalNode(children, 8);
            Console.WriteLine("Network building finished.");

            // learn
            level4.Learn();
            Console.WriteLine("Learning finished.");

            foreach (var v in level4.ClusterStream)
            {
                Console.Write($"{v}, ");
            }

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