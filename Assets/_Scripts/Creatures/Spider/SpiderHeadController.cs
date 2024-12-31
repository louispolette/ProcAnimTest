using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderHeadController : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _head;

    private SpiderMovement _spiderMovement;
    private Vector2? _positionToLookAt = null;

    private void Awake()
    {
        _spiderMovement = GetComponent<SpiderMovement>();
    }

    private void Update()
    {
        SetLookTargetToDestination();
        
        if (_positionToLookAt != null)
        {
            LookAt(_positionToLookAt.GetValueOrDefault());
        }
    }

    private void SetLookTargetToDestination()
    {
        if (_spiderMovement != null)
        {
            SetLookTarget(_spiderMovement.DestinationPosition);
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
