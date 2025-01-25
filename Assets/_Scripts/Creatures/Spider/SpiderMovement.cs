using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpiderMovement : MonoBehaviour
{
    [Header("Pathfinding")]

    [SerializeField] private Transform _targetTransform;

    [Space]

    [SerializeField] private float _stoppingDistance = 2f;

    [Header("Movement Parameters")]

    [SerializeField] private bool _movementEnabled = true;

    [Space]

    [SerializeField, Min(0)] private float _movementSpeed = 1.0f;
    [SerializeField, Min(0)] private float _standingForce = 1f;
    [SerializeField, Min(0)] private float _stepDistance = 0.5f;
    [SerializeField, Min(0)] private float _stepUpdateFrequency = 1f;

    
    [Header("Movement Tweaks")]

    [SerializeField, Min(0)] private float _stepFrequencyRange = 1f;
    [SerializeField, Min(0)] private float _stepDistanceRange = 1f;
    [SerializeField, Min(0)] private float _deviationRange = 1f;

    [Header("Debugging")]

    [SerializeField] private bool _enableDebug = false;

    private Rigidbody2D _rb;
    private Camera _camera;
    private SpiderLimbHandler _limbHandler;
    private Pathfinder _pathfinder;

    private Vector2 _targetStepPosition;

    private float _stepPositionUpdateTimer;
    private float _nextstepPositionUpdate;

    private bool _hasReachedDestination = false;

    /// <summary>
    /// Position that the spider is pathfinding to
    /// </summary>
    public Vector2 DestinationPosition => _pathfinder.DestinationNode.position;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _limbHandler = GetComponent<SpiderLimbHandler>();
        _pathfinder = GetComponent<Pathfinder>();
    }

    private void FixedUpdate()
    {
        CheckIfDestinationReached();
        HandleStepUpdate();
        Move();
    }

    #region movement

    private void Move()
    {
        DoStepForce();
        DoStandingForce();

        //Force that let's the spider take steps
        void DoStepForce()
        {
            if (!_movementEnabled) return;
            _rb.AddForce(GetAccelForce(_targetStepPosition, _movementSpeed));
            _rb.AddForce(GetBrakeForce(_targetStepPosition));
        }

        //Force that makes the spiders' legs affect its movement
        void DoStandingForce()
        {
            Vector2 averageLimbPos = _limbHandler.GetAverageLimbPosition();
            _rb.AddForce(GetAccelForce(averageLimbPos, _standingForce));
            _rb.AddForce(GetBrakeForce(averageLimbPos));
        }
    }

    /// <summary>
    /// Force applied that makes the rigidbody move towards a position
    /// </summary>
    /// <param name="accelTarget">Position that we're applying a focr towards</param>
    /// <param name="forceMult">Force multiplier</param>
    /// <returns></returns>
    private Vector2 GetAccelForce(Vector2 accelTarget, float forceMult = 1f)
    {
        Vector2 dir = (accelTarget - _rb.position).normalized;
        float speed = Vector2.Distance(_rb.position, accelTarget) * forceMult;

        Vector2 accelForce = dir * speed;

        return accelForce;
    }


    /// <summary>
    /// Calculates the force necessary to prevent the rigidbody from overshooting the chosen position
    /// </summary>
    /// <param name="brakeTarget">Position to brake from</param>
    /// <returns></returns>
    private Vector2 GetBrakeForce(Vector2 brakeTarget)
    {
        float velocityMag = _rb.velocity.magnitude;

        if (velocityMag == 0) return Vector2.zero;

        float distance = Vector2.Distance(_rb.position, brakeTarget);

        if (distance == 0) return Vector2.zero;

        Vector2 velocity = _rb.velocity;
        float mass = _rb.mass;

        Vector2 brakeForce = (mass * velocity) / (2 * distance / velocityMag);
        brakeForce = -brakeForce;

        return brakeForce;
    }

    #endregion

    #region checks

    private void CheckIfDestinationReached()
    {
        //_hasReachedDestination = Vector2.Distance(_rb.position, DestinationPosition) <= _stoppingDistance;
    }

    #endregion

    #region steps

    private void HandleStepUpdate()
    {
        if (_hasReachedDestination || _pathfinder.Path == null || _pathfinder.Path.Count <= 1) return;

        if (_stepPositionUpdateTimer >= _nextstepPositionUpdate)
        {
            SetNewStepPosition();
            MoveRandomLeg();
        }

        _stepPositionUpdateTimer += Time.deltaTime;
    }

    private void SetNewStepPosition()
    {
        _targetStepPosition = GetStepTowardsTarget();
        _stepPositionUpdateTimer = 0;
        _nextstepPositionUpdate = Mathf.Max(0f, Random.Range(_stepUpdateFrequency - _stepFrequencyRange / 2,
                                                             _stepUpdateFrequency + _stepFrequencyRange / 2));
    }

    private Vector2 GetStepTowardsTarget()
    {
        float randomDeviationAngle = Random.Range(-_deviationRange / 2, _deviationRange / 2);
        Vector2 baseDirection = (_pathfinder.GetNodeAtDistance(1).position - (Vector2)transform.position).normalized;
        Vector2 deviatedDir = Quaternion.AngleAxis(randomDeviationAngle, Vector3.forward) * baseDirection;

        float randomMaxDist = Random.Range(Mathf.Max(0, _stepDistance - _stepDistanceRange), _stepDistance + _stepDistanceRange);
        float stepDistance = Mathf.Min(Vector2.Distance(transform.position, DestinationPosition), randomMaxDist);

        Vector3 stepPosition = deviatedDir * stepDistance;

        return transform.position + stepPosition;
    }

    #endregion

    #region legs

    private void MoveRandomLeg()
    {
        var limbs = _limbHandler.Limbs;
        int randomLegIndex = Random.Range(0, limbs.Count);
        limbs[randomLegIndex].ForceMove();
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(DestinationPosition, _stoppingDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_targetStepPosition, 0.25f);
    }
}
