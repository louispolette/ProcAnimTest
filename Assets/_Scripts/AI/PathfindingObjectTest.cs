using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingObjectTest : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _target;

    [Header("Debug")]

    [SerializeField] private bool _drawGizmos = false;

    List<PathfindingNode> path;

    private void OnEnable()
    {
        PathfindingManager.OnPathfindingTick += PathfindingUpdate;
    }

    private void OnDisable()
    {
        PathfindingManager.OnPathfindingTick -= PathfindingUpdate;
    }

    private void PathfindingUpdate()
    {
        path = Pathfinding.FindPath(transform.position, _target.position);
    }

    private void FixedUpdate()
    {
        if (path == null || path.Count <= 1) return;

        transform.position = Vector2.MoveTowards(transform.position, path[1].position, 0.15f);
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
