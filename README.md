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

An implementation of Hierarchical Temporal Memory (HTM/Zeta1).

## Requirements
- .NET Framework >= 4.6.2
- Accord.NET >= 3.4.0

## Build
Just open the solution file, then build.

## How to use
Here is an example to build an HTM/Zeta1 network in HtmZetaOne. Use `LeafNode` for the 1st level nodes, and `InternalNode` for the 2nd or higher level nodes.

```csharp
var streams = new List<int[]>();
streams.Add(new[]{0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 2, 2, 2, 0});
streams.Add(new[]{0, 1, 0, 1, 2, 3, 2, 3, 2, 3, 0, 1, 0, 0});
streams.Add(new[]{0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1, 0});
streams.Add(new[]{1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 1});
streams.Add(new[]{5, 4, 5, 4, 2, 2, 2, 2, 2, 5, 4, 5, 4, 5});
var level1 = streams.Select(stream => new LeafNode(stream, null, 2));
var level2Left = new InternalNode(level1.Take(2), 2);  // Take the first two streams.
var level2Right = new InternalNode(level1.Skip(2), 2); // Take the last three streams.
var level3 = new InternalNode(new[] {level2Left, level2Right}, 2);
level3.Learn(); 
foreach(var value in level3.ClusterStream)
{
    Console.Write($"{value}, "); // output: 1, 1, 1, 1, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1
}
```

`InternalNode.Learn()` method calls its children's `Learn()` method recursively, so you have to write `Learn()` method only once. After learning, you will obtain the assignments for each data point to the clusters for each level of hierarchy. The cluster is also called *temporal group* in [the original paper](http://alpha.tmit.bme.hu/speech/docs/education/02_DileepThesis.pdf) by Dileep George.

Note: `LeafNode` can only take `IEnumerable<int>` as its argument.
This is because HTM/Zeta1 itself is highly dependent upon the discreteness of the input.
If you want to deal with more complex data, you must discretize your data and feed the indices to `LeafNode`.








