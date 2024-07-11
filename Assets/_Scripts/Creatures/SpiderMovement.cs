using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpiderMovement : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Movement Parameters")]

    [SerializeField] private bool enableMovement = true;

    [Space]

    [SerializeField, Min(0)] private float speed = 1.0f;
    [SerializeField, Min(0)] private float stepDistance = 0.5f;
    [SerializeField, Min(0)] private float stepUpdateFrequency = 1f;
    
    [Header("Randomness Settings")]

    [SerializeField, Min(0)] private float stepFrequencyRange = 1f;
    [SerializeField, Min(0)] private float stepDistanceRange = 1f;
    [SerializeField, Min(0)] private float deviationRange = 1f;

    [Header("Debugging")]

    [SerializeField] private bool enableDebug = false;

    private Vector3 _nextPosition;
    private Vector3 _smoothVelocity;
    private Coroutine _stepUpdater;

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, _nextPosition, ref _smoothVelocity, speed);
    }

    private Vector3 GetNextPosition()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Vector3 deviatedDir = Quaternion.AngleAxis(Random.Range(-deviationRange / 2, deviationRange / 2), Vector3.forward) * direction;

        float randomMaxDist = Random.Range(Mathf.Max(0, stepDistance - stepDistanceRange), stepDistance + stepDistanceRange);
        float distance = Mathf.Min(Vector2.Distance(transform.position, target.position), randomMaxDist);

        Vector3 position = deviatedDir * distance;

        return transform.position + position;
    }

    private IEnumerator UpdateNextPosition()
    {
        while (true)
        {
            _nextPosition = GetNextPosition();

            yield return new WaitForSeconds(Random.Range(stepUpdateFrequency - stepFrequencyRange / 2,
                                                         stepUpdateFrequency + stepFrequencyRange / 2));
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_nextPosition, 0.25f);
    }
}
