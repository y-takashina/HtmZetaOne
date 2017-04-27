using System;
using HtmZetaOne;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HtmZetaOneTests
{
    [TestClass()]
    public class NodeTests
    {
        private readonly Node _node;
        private readonly Node _tree;

        public NodeTests()
        {
            _node = new LeafNode(new[] {3, 4, 5, 4, 3, 4, 5, 8, 0, 0}, null, 2);
            _node.Learn();
            // Expected cluster:  0  0  0  0  0  0  0  0  0  0  1  1  1  0
            var stream1 = new[] {0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 0};
            //                    0  0  0  0  1  1  1  1  1  1  0  0  0  0
            var stream2 = new[] {0, 1, 0, 1, 2, 3, 2, 3, 2, 3, 0, 1, 0, 0};
            //                    0  0  0  0  0  1  0  1  0  1  1  1  1  0
            var stream3 = new[] {0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0};
            //                    0  0  0  0  1  2  1  2  1  2  3  3  3  0,
            //                    where 0:000, 1:010, 2:011, 3:101
            _tree = new InternalNode(new[]
            {
                new LeafNode(stream1, stream1, 2),
                new LeafNode(stream2, stream2, 2),
                new LeafNode(stream3, stream3, 2),
            }, 2);
            _tree.Learn();
        }

        [TestMethod()]
        public void ForwardHardTest()
        {
            var inputs = new[] {0, 1, 2, 3, 4};
            var answers = new[] {1, 1, 1, 0, 0};
            for (var i = 0; i < 5; i++)
            {
                var output = _node.Forward(inputs[i]);
                Assert.AreEqual(answers[i], output);
            }
        }

        [TestMethod()]
        public void ForwardSoftTest()
        {
            var inputs = new[]
            {
                new[] {0.2, 0.7, 0.0, 0.0, 0.1},
                new[] {0.0, 0.0, 0.2, 0.6, 0.2},
                new[] {0.2, 0.2, 0.2, 0.2, 0.2},
            };
            var answers = new[,] {{0.125, 0.875}, {0.75, 0.25}, {0.5, 0.5}};
            for (var i = 0; i < 3; i++)
            {
                var output = _node.Forward(inputs[i]);
                for (var j = 0; j < 2; j++)
                {
                    Assert.AreEqual(answers[i, j], output[j], 1e-6);
                }
            }
        }

        [TestMethod()]
        public void BackwardHardTest()
        {
            var inputs = new[] {0, 1};
            var answers = new[,] {{0, 0, 0, 1, 1}, {1, 1, 1, 0, 0}};
            for (var i = 0; i < 2; i++)
            {
                var output = _node.Backward(inputs[i]);
                for (var j = 0; j < 5; j++)
                {
                    Assert.AreEqual(answers[i, j], output[j]);
                }
            }
        }

        [TestMethod()]
        public void BackwardSoftTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void LearnTest()
        {
            var answers = new[,] {{0, 1}, {0, 1}, {0, 1}, {1, 0}, {1, 0}};
            for (var i = 0; i < 5; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    Assert.AreEqual(answers[i, j], _node.Membership[i, j]);
                }
            }
        }

        [TestMethod()]
        public void LearnInternalNodeTest()
        {
            var answers = new[,] {{0, 1}, {1, 0}, {1, 0}, {0, 1}};
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    Assert.AreEqual(answers[i, j], _tree.Membership[i, j]);
                }
            }
        }

        [TestMethod()]
        public void PredictTest()
        {
            foreach (var value in _tree.ClusterStream)
            {
                var prediction = _tree.Predict();
                for (var i = 0; i < prediction.Length; i++)
                {
                    Assert.AreEqual(i == value ? 1 : 0, prediction[i], double.Epsilon);
                }
            }
        }
    }
}