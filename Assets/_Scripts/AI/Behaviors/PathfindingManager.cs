using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingManager : MonoBehaviour
{
    public static PathfindingManager Instance;

    [Header("Node Creation")]

    [SerializeField] private Vector2 _gridOrigin;

    [Space]

    [SerializeField, Min(0f)] private float _nodeSpacing = 1f;
    [SerializeField] private LayerMask _obstacleDetectionMask;

    [Space]

    [SerializeField] private float _gridWidth = 50f;
    [SerializeField] private float _gridHeight = 50f;

    [Header("Performance")]

    [SerializeField] private float _pathfindingTickrate = 0.1f;

    [Header("Debug")]

    [SerializeField] private bool _gizmosEnabled = false;

    [Space]

    [SerializeField] private bool _drawNodes = true;
    [SerializeField] private bool _drawNavigationArea = true;

    public static event Action OnPathfindingTick;

    private float _tickTimer = 0f;

    public Dictionary<Vector2, PathfindingNode> Tiles { get; private set; } = new Dictionary<Vector2, PathfindingNode>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Tiles = GenerateGrid();
    }

    private void Update()
    {
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= _pathfindingTickrate)
        {
            _tickTimer = 0f;
            OnPathfindingTick?.Invoke();
        }
    }

    private Dictionary<Vector2, PathfindingNode> GenerateGrid()
    {
        if (_nodeSpacing <= 0f) return null;

        var tiles = new Dictionary<Vector2, PathfindingNode>();

        float xPosition = _gridOrigin.x;

        while (xPosition <= _gridOrigin.x + _gridWidth)
        {
            float yPosition = _gridOrigin.y;

            while (yPosition <= _gridOrigin.y + _gridHeight)
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
        var hitCollider = Physics2D.OverlapCircle(position, 0f, _obstacleDetectionMask);

        return hitCollider == null;
    }

    public List<PathfindingNode> GetNeighbors(PathfindingNode node)
    {
        List<PathfindingNode> neighbors = new List<PathfindingNode>();
        float unit = _nodeSpacing;

        for (float x = -unit; x <= unit; x += unit)
        {
            for (float y = -unit; y <= unit; y += unit)
            {
                if (x == 0 && y == 0) continue;

                float checkX = node.position.x + x;
                float checkY = node.position.y + y;

                if (checkX >= _gridOrigin.x && checkX < _gridOrigin.x + _gridWidth && checkY >= _gridOrigin.y && checkY < _gridOrigin.y + _gridHeight)
                {
                    neighbors.Add(Tiles[new Vector2(checkX, checkY)]);
                }
            }
        }

        return neighbors;
    }

    public float GetDistance(PathfindingNode nodeA, PathfindingNode nodeB)
    {
        return Vector2.Distance(nodeA.position, nodeB.position);
    }

    public PathfindingNode GetNodeFromWorldPosition(Vector2 worldPosition)
    {
        Vector2 clampedPosition = new Vector2(Mathf.Clamp(worldPosition.x, _gridOrigin.x, _gridOrigin.x + _gridWidth),
                                              Mathf.Clamp(worldPosition.y, _gridOrigin.y, _gridOrigin.y + _gridHeight));

        float snappedX = GetClosestNumberFromStep(clampedPosition.x, _nodeSpacing);
        float snappedY = GetClosestNumberFromStep(clampedPosition.y, _nodeSpacing);

        if (Tiles.TryGetValue(new Vector2(snappedX, snappedY), out PathfindingNode foundNode))
        {
            return foundNode;
        }
        else
        {
            Debug.LogError($"Couldn't find node at [{snappedX},{snappedY}]");
            return null;
        }

        float GetClosestNumberFromStep(float value, float step)
        {
            // Get the absolute values of our arguments
            var absValue = Mathf.Abs(value);
            step = Mathf.Abs(step);

            // Determing the numbers on either side of value
            var low = absValue - absValue % step;
            var high = low + step;

            // Return the closest one, multiplied by -1 if value < 0
            var result = absValue - low < high - absValue ? low : high;
            return result * Mathf.Sign(value);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!_gizmosEnabled) return;

        DrawNavigationArea();
        DrawNodes();
        
        void DrawNodes()
        {
            if (!_drawNodes || Tiles.Count <= 0) return;

            foreach (var tile in Tiles)
            {
                Gizmos.color = tile.Value.Accessible ? Color.green : Color.red;
                Gizmos.DrawSphere(tile.Key, _nodeSpacing * 0.15f);
            }
        }

        void DrawNavigationArea()
        {
            if (!_drawNavigationArea) return;

            Gizmos.color = Color.white;
            Vector2 boxCenter = new Vector2(_gridOrigin.x + _gridWidth / 2f, _gridOrigin.y + _gridHeight / 2f);
            Gizmos.DrawWireCube(boxCenter, new Vector2(_gridWidth, _gridHeight));
        }
    }
}
