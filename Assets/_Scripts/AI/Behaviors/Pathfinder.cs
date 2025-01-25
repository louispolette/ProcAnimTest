using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _target;

    [Header("Debug")]

    [SerializeField] private bool _drawGizmos = false;

    public List<PathfindingNode> Path { get; private set; } = new List<PathfindingNode>();

    public PathfindingNode DestinationNode => GetNodeAtDistance(Path.Count - 1);

    private void OnEnable()
    {
        PathfindingManager.OnPathfindingTick += DoPathfindingUpdate;
    }

    private void OnDisable()
    {
        PathfindingManager.OnPathfindingTick -= DoPathfindingUpdate;
    }

    private void DoPathfindingUpdate()
    {
        Path = Pathfinding.FindPath(transform.position, _target.position);
        if (Path.Count == 0)
        {
            Debug.Log("NO PATH");
        }
    }

    /// <summary>
    /// Returns the PathfindingNode0 located at a certain amount of nodes away from the starting node
    /// </summary>
    /// <param name="distance">Distance (how many nodes in between) from the starting node to the returned node</param>
    /// <returns></returns>
    public PathfindingNode GetNodeAtDistance(int distance)
    {
        //if (Path == null) return null;
        //if (distance >= Path.Count) return null;

        return Path[distance];
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        if (Path == null) return;

        Gizmos.color = Color.white;

        Vector3 pointA;
        Vector3 pointB;
        
        for (int i = 0; i < Path.Count - 1; i++)
        {
            pointA = Path[i].position;
            pointB = Path[i+1].position;

            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
