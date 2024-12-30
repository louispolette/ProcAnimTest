using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpiderMovement : MonoBehaviour
{
    [SerializeField] private SpiderMovementMode _movementMode = SpiderMovementMode.MoveTowardsTranform;
    [SerializeField] private Transform _targetTransform;

    [Header("Movement Parameters")]

    [SerializeField, Min(0)] private float _movementSpeed = 1.0f;
    [SerializeField, Range(0f, 1f)] private float _gravityResistForce = 1f;
    [SerializeField, Min(0)] private float _stepDistance = 0.5f;
    [SerializeField, Min(0)] private float _stepUpdateFrequency = 1f;
    
    [Header("Randomness Settings")]

    [SerializeField, Min(0)] private float _stepFrequencyRange = 1f;
    [SerializeField, Min(0)] private float _stepDistanceRange = 1f;
    [SerializeField, Min(0)] private float _deviationRange = 1f;

    [Header("Debugging")]

    [SerializeField] private bool _enableDebug = false;

    private Rigidbody2D _rb;
    private Camera _camera;

    private Vector2 _targetStepPosition;
    private Vector2 _smoothVelocity;
    private Vector2 _previousVelocity;
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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (_movementMode == SpiderMovementMode.FollowClick)
        {
            HandleClicks();
        }
    }

    private void FixedUpdate()
    {
        HandleStepFinding();
        Move();

        _previousVelocity = _rb.velocity;
    }

    private void Move()
    {
        //transform.position = Vector2.SmoothDamp(transform.position, _targetStep, ref _smoothVelocity, 1f / _speed);

        _rb.AddForce(GetAccelForce());
        _rb.AddForce(GetBrakeForce());
        _rb.velocity += -Physics2D.gravity * Time.deltaTime * _gravityResistForce;
    }


    private Vector2 GetAccelForce()
    {
        Vector2 dir = (_targetStepPosition - _rb.position).normalized;
        float speed = Vector2.Distance(_rb.position, _targetStepPosition) * _movementSpeed;

        Vector2 accelForce = dir * speed;

        return accelForce;
    }

    private Vector2 GetBrakeForce()
    {
        float distance = Vector2.Distance(_rb.position, _targetStepPosition);
        float velocityMag = _rb.velocity.magnitude;
        Vector2 velocity = _rb.velocity;
        float mass = _rb.mass;

        Vector2 brakeForce = (mass * velocity) / (2 * distance / velocityMag);
        brakeForce = -brakeForce;

        return brakeForce;
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
            _targetStepPosition = GetStepTowardsTarget();
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
        Gizmos.DrawSphere(_targetStepPosition, 0.25f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(TargetPosition, 0.40f);
    }
}
