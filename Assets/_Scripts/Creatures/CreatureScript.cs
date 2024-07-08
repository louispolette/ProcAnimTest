using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

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

    [SerializeField] private Bone rootBone;
    [SerializeField] private GameObject legPrefab;
    [SerializeField] private int legAmount;

    [Header("Debugging")]

    [SerializeField] private bool enableDebug = false;

    private Vector3 _nextPosition;
    private Vector3 _smoothVelocity;
    private Coroutine _stepUpdater;
    private Limb[] _limbs;

    private void Awake()
    {
        _stepUpdater = StartCoroutine(UpdateNextPosition());
    }

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, _nextPosition, ref _smoothVelocity, speed);
    }

    /// <summary>
    /// Sets up the limbs of the creature
    /// </summary>
    private void CreateLimbs()
    {
        Bone[] limbRoots = GetDirectChildBones(rootBone);

        for (int i = 0; i < legAmount; i++)
        {
            _limbs[i] = new Limb()
        }
    }

    /// <summary>
    /// Returns the direct child bones of a bone
    /// </summary>
    /// <param name="bone"></param>
    /// <returns></returns>
    private Bone[] GetDirectChildBones(Bone bone)
    {
        if (bone.transform.childCount == 0) // If the bone is an end bone
        {
            bone.isEndBone = true;
            return null;
        }

        List<Bone> childBones = new();

        for (int i = 0; i < bone.transform.childCount; ++i)
        {
            Bone child = transform.GetChild(i).GetComponent<Bone>();

            if (child != null)
            {
                childBones.Add(child);
            }
        }

        return childBones.ToArray();
    }

    private void AddChildBones(Transform parent, List<Bone> list)
    {
        if (parent.transform.childCount == 0)
        {
            if (parent.TryGetComponent(out Bone bone))
            {
                bone.isEndBone = true;
                return;
            }
        }

        foreach (Transform child in parent.transform)
        {
            if (child.TryGetComponent(out Bone bone))
            {
                list.Add(bone);
                AddChildBones(child, list);
            }
        }
    }

    /// <summary>
    /// Builds a limb with every children of the selected bone
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    private Limb BuildLimb(Bone rootBone)
    {
        List<Bone> limbBones = new();

        AddChildBones(rootBone.transform, limbBones);

        return new Limb(limbBones.ToArray());
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
