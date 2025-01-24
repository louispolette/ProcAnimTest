using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private static PathfindingManager PathfindingManager => PathfindingManager.Instance;
    private static Dictionary<Vector2, PathfindingNode> Nodes => PathfindingManager.Instance.Tiles;

    public static List<PathfindingNode> FindPath(PathfindingNode startNode, PathfindingNode endNode)
    {
        if (PathfindingManager.Instance == null)
        {
            Debug.LogError("No PathfindingManager has been found in the scene");
            return null;
        }

        List<PathfindingNode> openSet = new List<PathfindingNode>();
        HashSet<PathfindingNode> closedSet = new HashSet<PathfindingNode>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            PathfindingNode currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].F < currentNode.F 
                    || openSet[i].F == currentNode.F && openSet[i].H < currentNode.H)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == endNode)
            {
                List<PathfindingNode> path = RetracePath();
                return path;
            }

            foreach (PathfindingNode neighbor in PathfindingManager.GetNeighbors(currentNode))
            {
                if (!neighbor.Accessible || closedSet.Contains(neighbor))
                {
                    continue;
                }

                float newMovementCostToNeighbor = currentNode.G + PathfindingManager.GetDistance(currentNode, neighbor);

                if (newMovementCostToNeighbor < neighbor.G || !openSet.Contains(neighbor))
                {
                    neighbor.SetG(newMovementCostToNeighbor);
                    neighbor.SetH(PathfindingManager.GetDistance(neighbor, endNode));
                    neighbor.SetConnection(currentNode);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null;

        List<PathfindingNode> RetracePath()
        {
            List<PathfindingNode> path = new List<PathfindingNode>();
            PathfindingNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.Connection;
            }

            path.Reverse();
            return path;
        }
    }

    public static List<PathfindingNode> FindPath(Vector2 startNodeWorldPos, Vector2 endNodeWorldPos)
    {
        PathfindingNode startNode = PathfindingManager.GetNodeFromWorldPosition(startNodeWorldPos);
        PathfindingNode endNode = PathfindingManager.GetNodeFromWorldPosition(endNodeWorldPos);

        return FindPath(startNode, endNode);
    }
}
