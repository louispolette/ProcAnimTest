using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatureScript : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Movement Parameters")]

    [SerializeField, Min(0)] private float speed = 1.0f;
    [SerializeField, Min(0)] private float stepDistance = 0.5f;
    [SerializeField, Min(0)] private float stepUpdateFrequency = 1f;
    
    [Header("Randomness Settings")]

    [SerializeField, Min(0)] private float stepFrequencyRange = 1f;
    [SerializeField, Min(0)] private float stepDistanceRange = 1f;
    [SerializeField, Min(0)] private float deviationRange = 1f;

    [Header("Legs")]

    [SerializeField] private int legAmount;

    [Header("Debugging")]

    [SerializeField] private bool enableDebug = false;

    private Vector3 nextPosition;
    private Vector3 smoothVelocity;
    private Coroutine stepUpdater;
    private List<Leg> legs = new List<Leg>();

    private void Awake()
    {
        stepUpdater = StartCoroutine(UpdateNextPosition());
    }

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, nextPosition, ref smoothVelocity, speed);
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
            nextPosition = GetNextPosition();

            yield return new WaitForSeconds(Random.Range(stepUpdateFrequency - stepFrequencyRange / 2,
                                                         stepUpdateFrequency + stepFrequencyRange / 2));
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(nextPosition, 0.25f);
    }

    public class Leg
    {
        public Vector3 position;

        public Leg()
        {
            position = Vector3.zero;
        }
    }
}
