using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderHeadController : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _head;

    private Pathfinder _pathfinder;

    private Vector2? _positionToLookAt = null;

    private void Awake()
    {
        _pathfinder = GetComponent<Pathfinder>();
    }

    private void Update()
    {
        UpdateLookTarget();
        
        if (_positionToLookAt != null)
        {
            LookAt(_positionToLookAt.GetValueOrDefault());
        }
    }

    private void UpdateLookTarget()
    {
        if (_pathfinder != null && _pathfinder.Path.Count > 0)
        {
            SetLookTarget(_pathfinder.DestinationNode.position);
        }
    }

    public void SetLookTarget(Vector2 targetPosition)
    {
        _positionToLookAt = targetPosition;
    }

    private void LookAt(Vector2 lookTarget)
    {
        Vector2 dir = ((Vector2)_head.position - lookTarget).normalized;
        _head.up = dir;
    }
}
