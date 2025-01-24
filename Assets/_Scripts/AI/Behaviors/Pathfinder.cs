using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _target;

    [Header("Debug")]

    [SerializeField] private bool _drawGizmos = false;

    List<PathfindingNode> path;

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
        path = Pathfinding.FindPath(transform.position, _target.position);
    }

    /// <summary>
    /// Returns the PathfindingNode0 located at a certain amount of nodes away from the starting node
    /// </summary>
    /// <param name="distance">Distance (how many nodes in between) from the starting node to the returned node</param>
    /// <returns></returns>
    public PathfindingNode GetNodeAtDistance(int distance)
    {
        if (path == null || path.Count <= 1) return null;

        return path[distance];
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos) return;

        if (path == null) return;

        Gizmos.color = Color.white;

        Vector3 pointA;
        Vector3 pointB;
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            pointA = path[i].position;
            pointB = path[i+1].position;

            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
