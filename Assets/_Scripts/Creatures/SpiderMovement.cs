using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpiderMovement : MonoBehaviour
{
    [SerializeField] private Transform _target;

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

    private Vector3 _nextPosition;
    private Vector3 _smoothVelocity;
    private Coroutine _stepUpdater;

    private void Start()
    {
        _stepUpdater = StartCoroutine(UpdateNextPosition());
    }

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, _nextPosition, ref _smoothVelocity, 1f / _speed);
    }

    private Vector3 GetNextPosition()
    {
        Vector3 direction = (_target.position - transform.position).normalized;
        Vector3 deviatedDir = Quaternion.AngleAxis(Random.Range(-_deviationRange / 2, _deviationRange / 2), Vector3.forward) * direction;

        float randomMaxDist = Random.Range(Mathf.Max(0, _stepDistance - _stepDistanceRange), _stepDistance + _stepDistanceRange);
        float distance = Mathf.Min(Vector2.Distance(transform.position, _target.position), randomMaxDist);

        Vector3 position = deviatedDir * distance;

        return transform.position + position;
    }

    private IEnumerator UpdateNextPosition()
    {
        while (true)
        {
            _nextPosition = GetNextPosition();

            yield return new WaitForSeconds(Random.Range(_stepUpdateFrequency - _stepFrequencyRange / 2,
                                                         _stepUpdateFrequency + _stepFrequencyRange / 2));
        }
    }

    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_nextPosition, 0.25f);
    }
}
