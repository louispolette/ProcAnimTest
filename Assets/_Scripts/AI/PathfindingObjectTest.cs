using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingObjectTest : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _target;

    List<PathfindingNode> path;

    private void FixedUpdate()
    {
        path = Pathfinding.FindPath(transform.position, _target.position);
    }

    private void OnDrawGizmos()
    {
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
