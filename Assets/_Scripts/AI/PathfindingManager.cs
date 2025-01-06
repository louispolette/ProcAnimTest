using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingManager : MonoBehaviour
{
    public static PathfindingManager Instance;

    [Space]

    [SerializeField] private Vector2 _gridOrigin;

    [Space]

    [SerializeField, Min(0f)] private float _nodeSpacing = 1f;

    [Space]

    [SerializeField] private int _gridWidth = 50;
    [SerializeField] private int _gridHeight = 50;

    public Dictionary<Vector2, PathfindingNode> Tiles { get; private set; } = new Dictionary<Vector2, PathfindingNode>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Tiles = GenerateGrid();
    }

    private Dictionary<Vector2, PathfindingNode> GenerateGrid()
    {
        var tiles = new Dictionary<Vector2, PathfindingNode>();

        float xPosition = _gridOrigin.x;

        for (int x = 0; x < _gridWidth; x++)
        {
            float yPosition = _gridOrigin.y;

            for (int y = 0; y < _gridHeight; y++)
            {
                var newNodePosition = new Vector2(xPosition, yPosition);
                var newNode = new PathfindingNode(IsNodeAccessible(newNodePosition), newNodePosition);

                tiles.Add(newNodePosition, newNode);

                yPosition += _nodeSpacing;
            }

            xPosition += _nodeSpacing;
        }

        return tiles;
    }

    private bool IsNodeAccessible(Vector2 position)
    {
        return true;
    }

    private void OnDrawGizmos()
    {
        if (Tiles.Count <= 0) return;

        foreach (var tile in Tiles)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(tile.Key, 0.1f);
        }
    }
}
