using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PathfindingNode
{
    public PathfindingNode Connection { get; private set; }
    public float G { get ; private set; }
    public float H { get ; private set; }
    public float F => G + H;

    public bool Walkable { get; private set; }
    public Vector2 position { get; private set; }

    public void SetConnection(PathfindingNode node) => Connection = node;

    public void SetG(float g) => G = g;
    public float SetH(float h) => H = h;

    public PathfindingNode(bool walkable, Vector2 position)
    {
        Walkable = walkable;
        this.position = position;
    }
}
