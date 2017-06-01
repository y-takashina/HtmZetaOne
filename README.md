# HtmZetaOne

<!--
Zeta1 algorithm is a nonparametric clustering algorithm for sequences. 
Zeta1 clusters each point from a series under the following two assumptions.
- Compositionality
- Time invariance / Slowness

Since Zeta1 treats the learned clusters as random variables, Zeta1 can also be understood as Bayesian network.
The original paper by Dileep George is available [here](http://alpha.tmit.bme.hu/speech/docs/education/02_DileepThesis.pdf).

Note: HTM/Zeta1 is completely different from Cortical Learning Algorithm (HTM/CLA) theoretically. Since some insist HTM/CLA is an improved version of HTM/Zeta1, HTM/CLA doesn't have hierarchy and time invariance, which play critically important role in HTM/Zeta1.
-->

An implementation of Hierarchical Temporal Memoy (HTM/Zeta1).

## Requirements
- .NET Framework >= 4.6.2
- Accord.NET >= 3.4.0

## Build
Just open the solution file, then build.
r
## Correspondence to the theory
- HTM Node
  - In this repository, a `Node` represents an HTM Node. `LeafNode` and `LeafNodeForContinuous` are for the 1st level nodes, and `InternalNode` is for the 2nd or higher level nodes.
  - The 1st argument of the constructor of `LeafNode` and `LeafNodeForContinuous` is train stream, and the 2nd is test stream. The 3rd argument is the number of temporal groups. The 4th is a metrics, which is used to calculate distance in the temporal pooling process.
  - The 1st argument of the constructor of `InternalNode` is child nodes, the 2nd is the number of temporal groups, and the 3rd is the metrics described before.
- Coincidence patterns
  - The coincidence patterns in each node are stored in the node's `SpatialPooler`.
- Temporal groups
  - The temporal groups which each data point in the train stream belongs to are stored in the node's `ClusterStream`.

## Examples
### Feedforward example (clustering)
To test the feedforward computation in HTM/Zeta1, you can build an HTM/Zeta1 network by simply aggregating data streams as you like. After learning, you will obtain the assignments for each data point to the clusters for each level of hierarchy. (They are in `HtmZetaOne.Node.ClusterStream`.) The clusters are also called *temporal groups* in [the original paper](http://alpha.tmit.bme.hu/speech/docs/education/02_DileepThesis.pdf) by Dileep George.

```csharp
var streams = new List<int[]>();
streams.Add(new[]{0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 0});
streams.Add(new[]{0, 1, 0, 1, 2, 3, 2, 3, 2, 3, 0, 1, 0, 0});
streams.Add(new[]{0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0});
streams.Add(new[]{5, 4, 5, 4, 2, 2, 2, 2, 2, 5, 4, 5, 4, 5});
// The 1st argument is training data, and the 2nd argument is test data.
// The 3rd argument is the number of temporal groups in the node.
var level1 = streams.Select(stream => new LeafNode(stream, null, 2));
var level2Left = new InternalNode(level1.Take(2), 2);  // Take the first two streams.
var level2Right = new InternalNode(level1.Skip(2), 2); // Take the last two streams.
var root = new InternalNode(new[] {level2Left, level2Right}, 2);
root.Learn(); 
foreach(var value in root.ClusterStream)
{
    Console.Write($"{value}, "); // output: 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1
}
```

### Feedback example (classification)
The network structure in this subsection is almost the same as the previous one, but a node for the label is added at the top hierarchy. Moreover, each stream is divided into two parts, a train part and a test part. After learning, `Node.Predict()` fetches the results of its children's `Predict` methods recursively, and finally get the predicted cluster at the top node. Then, putting the predicted value into `Node.Generate()`, you can obtain the generated states in every node. 

```csharp
var streams = new List<int[]>
{
    new[] {0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 0},
    new[] {0, 1, 0, 1, 2, 3, 2, 3, 2, 3, 0, 1, 0, 0},
    new[] {0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0},
    new[] {5, 4, 5, 4, 2, 2, 2, 2, 2, 5, 4, 5, 4, 5}
};
// Take the first ten points for training, leaving the last four points for testing.
var level1 = streams.Select(stream => new LeafNode(stream.Take(10), stream.Skip(10), 2));
var level2Left = new InternalNode(level1.Take(2), 2); // Take the first two streams.
var level2Right = new InternalNode(level1.Skip(2), 2); // Take the last two streams.
var root = new InternalNode(new[] {level2Left, level2Right}, 2);

var labelStream = new[] {1, 1, 1, 0, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1};
// To test the prediction accuracy for label, input a stream filled with `-1` into the second argument.
var labelNode = new LeafNode(labelStream.Take(10), Enumerable.Repeat(-1, 4), 2);
var superRoot = new InternalNode(new Node[] {root, labelNode}, 2);
superRoot.Learn();

while (superRoot.CanPredict)
{
    var predicted = superRoot.Predict();
    superRoot.Generate(predicted);
}
var accuracy = (double) labelNode.GeneratedStream.Zip(labelStream.Skip(10), (predicted, actual) => predicted == actual ? 1 : 0).Sum();
accuracy /= labelNode.GeneratedStream.Count;
Console.WriteLine($"accuracy: {accuracy}");
```

Note: `LeafNode` can only take `IEnumerable<int>` as its argument. This is because HTM/Zeta1 itself is highly dependent upon the discreteness of the input. If you want to deal with more complex data (e.g. images), you must discretize your data and feed the gained indices to `LeafNode`. **Only in the 1-dimensional case**, `HtmZetaOne` provides `LeafNodeForContinuous` class to deal with continuous inputs. This enables fuzzy matching between memorized patterns and new inputs.

These demos are in `ToyProblems.cs` [here](https://github.com/y-takashina/HtmZetaOne/blob/master/HtmZetaOneDemos/).









