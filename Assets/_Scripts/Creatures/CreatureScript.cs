using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CreatureScript : MonoBehaviour
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

    [Header("Legs")]

    public Bone legRoot;
    [SerializeField] private GameObject legPrefab;
    [SerializeField] private int legAmount;

    [Header("Debugging")]

    [SerializeField] private bool enableDebug = false;

    public List<Limb> _limbs { get; private set; } = new List<Limb>();

    private Vector3 _nextPosition;
    private Vector3 _smoothVelocity;
    private Coroutine _stepUpdater;

    private void Awake()
    {
        if (enableMovement)
        {
            _stepUpdater = StartCoroutine(UpdateNextPosition());
        }

        CreateLimbs();
    }

    private void Update()
    {
        transform.position = Vector3.SmoothDamp(transform.position, _nextPosition, ref _smoothVelocity, speed);
    }

    #region rig building

    /// <summary>
    /// Sets up the limbs of the creature
    /// </summary>
    private void CreateLimbs()
    {
        Bone[] limbRoots = GetDirectChildBones(legRoot);

        foreach (Bone limbStart in limbRoots)
        {
            _limbs.Add(BuildLimb(limbStart));
        }
    }

    /// <summary>
    /// Builds a limb from every children of the selected bone
    /// </summary>
    /// <param name="limbStart">First bone of the limb that isn't the root bone</param>
    /// <returns></returns>
    private Limb BuildLimb(Bone limbStart)
    {
        List<Bone> limbBones = new();

        limbBones.Add(limbStart);

        GetAllChildBones(limbStart.transform, limbBones);

        return new Limb(limbBones.ToArray());
    }

    /// <summary>
    /// Returns the direct child bones of a bone
    /// </summary>
    /// <param name="bone"></param>
    /// <returns></returns>
    private Bone[] GetDirectChildBones(Bone bone)
    {
        List<Bone> childBones = new();

        bool hasChildBone = false;

        foreach (Transform child in bone.transform)
        {
            if (child.TryGetComponent(out Bone childBone))
            {
                hasChildBone = true;
                childBones.Add(childBone);
            }
        }

        bone.isEndBone = !hasChildBone;

        return childBones.ToArray();
    }

    /// <summary>
    /// Recursively gets all child bones of the given parent and puts them into the given list
    /// </summary>
    /// <param name="parent">Transform from which to get get bones</param>
    /// <param name="list">List that holds the child bones</param>
    private void GetAllChildBones(Transform parent, List<Bone> list)
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
            if (child.TryGetComponent(out Bone childBone))
            {
                list.Add(childBone);
                GetAllChildBones(child, list);
            }
        }
    }

    #endregion

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
