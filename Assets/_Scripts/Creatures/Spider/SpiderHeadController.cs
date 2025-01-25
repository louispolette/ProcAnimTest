using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderHeadController : MonoBehaviour
{
    [Space]

    [SerializeField] private Transform _head;

    [Space]

    [SerializeField] private float _rotationSpeed = 1f;

    private Pathfinder _pathfinder;

    private Vector2? _positionToLookAt = null;

    private void Awake()
    {
        _pathfinder = GetComponent<Pathfinder>();
    }

    // TO DO : Make head look directly at player transform if in line of sight, otherwise look at last seen position

    private void Update()
    {
        UpdateLookTarget();
        
        if (_positionToLookAt != null)
        {
            RotateHeadTowardsTarget();
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

    private void RotateHeadTowardsTarget()
    {
        Vector3 dir = (Vector3)_positionToLookAt.GetValueOrDefault() - _head.position;

        Quaternion rot = Quaternion.Slerp(_head.rotation, Quaternion.LookRotation(dir, Vector3.forward), Time.deltaTime * _rotationSpeed);

        rot.x = 0f;
        rot.y = 0f;

        _head.rotation = rot;
    }
}
