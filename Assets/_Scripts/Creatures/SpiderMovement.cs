using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpiderMovement : MonoBehaviour
{
    [SerializeField] private SpiderMovementMode _movementMode = SpiderMovementMode.MoveTowardsTranform;
    [SerializeField] private Transform _targetTransform;

    [Header("Movement Parameters")]

    [SerializeField, Min(0)] private float _speed = 1.0f;
    [SerializeField, Min(0)] private float _stepDistance = 0.5f;
    [SerializeField, Min(0)] private float _stepUpdateFrequency = 1f;
    
    [Header("Randomness Settings")]

    [SerializeField, Min(0)] private float _stepFrequencyRange = 1f;
    [SerializeField, Min(0)] private float _stepDistanceRange = 1f;
    [SerializeField, Min(0)] private float _deviationRange = 1f;

    [Header("Debugging")]

    [SerializeField] private bool _enableDebug = false;

    private Camera _camera;

    private Vector2 _targetStep;
    private Vector2 _smoothVelocity;
    private Vector2 _lastClickedPosition;
    private Coroutine _stepUpdater;
    private float _stepPositionUpdateTimer;
    private float _nextstepPositionUpdate;

    private Vector2 TargetPosition
    {
        get
        {
            switch (_movementMode)
            {
                case SpiderMovementMode.MoveTowardsTranform:
                    return _targetTransform.position;
                case SpiderMovementMode.FollowClick:
                    return _lastClickedPosition;
                default:
                    return transform.position;
            }
        }
    }

    private Camera Camera
    {
        get
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            return _camera;
        }
    }

    private enum SpiderMovementMode
    {
        None,
        MoveTowardsTranform,
        FollowClick
    }

    private void Update()
    {
        if (_movementMode == SpiderMovementMode.FollowClick)
        {
            HandleClicks();
        }

        HandleStepFinding();
        Move();
    }

    private void Move()
    {
        transform.position = Vector2.SmoothDamp(transform.position, _targetStep, ref _smoothVelocity, 1f / _speed);
    }

    private void HandleClicks()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _lastClickedPosition = Camera.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void HandleStepFinding()
    {
        if (_stepPositionUpdateTimer >= _nextstepPositionUpdate)
        {
            _targetStep = GetStepTowardsTarget();
            _stepPositionUpdateTimer = 0;
            _nextstepPositionUpdate = Mathf.Max(0f, Random.Range(_stepUpdateFrequency - _stepFrequencyRange / 2,
                                                                 _stepUpdateFrequency + _stepFrequencyRange / 2));
        }

        _stepPositionUpdateTimer += Time.deltaTime;
    }

    private Vector2 GetStepTowardsTarget()
    {
        float randomDeviationAngle = Random.Range(-_deviationRange / 2, _deviationRange / 2);
        Vector2 baseDirection = (TargetPosition - (Vector2)transform.position).normalized;
        Vector2 deviatedDir = Quaternion.AngleAxis(randomDeviationAngle, Vector3.forward) * baseDirection;

        float randomMaxDist = Random.Range(Mathf.Max(0, _stepDistance - _stepDistanceRange), _stepDistance + _stepDistanceRange);
        float stepDistance = Mathf.Min(Vector2.Distance(transform.position, _targetTransform.position), randomMaxDist);

        Vector3 stepPosition = deviatedDir * stepDistance;

        return transform.position + stepPosition;
    }

    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetStep, 0.25f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(TargetPosition, 0.40f);
    }
}
