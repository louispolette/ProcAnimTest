using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class SpiderLimbScript : MonoBehaviour
{
    #region serialized

    [Header("Legs")]

    [Tooltip("Shared parent of all limbs")]
    public Bone limbBase;

    [SerializeField] private int legAmount;

    [SerializeField] private float legLength;

    [Tooltip("How much should the spider's base position extend its legs, 1 is fully extended")]
    [SerializeField, Range(0f, 1f)] private float legBaseDistance = 0.75f;

    [Space]

    [Tooltip("Parent that will contain all IK Solvers")]
    [SerializeField] Transform solversParent;

    [Tooltip("Parent that will contain all IK Targets")]
    [SerializeField] Transform targetsParent;

    [Header("Debug")]

    [SerializeField] private bool _enableDebug = false;

    #endregion

    #region not serialized

    public List<Limb> _limbs { get; private set; } = new List<Limb>();

    private List<Vector3> _idealLegPositions = new List<Vector3>();

    private IKManager2D _IKManager;

    private Bone[] bones;

    #endregion

    private void Awake()
    {
        _IKManager = GetComponentInChildren<IKManager2D>();

        LimbSetup();
        GetIdealLegPositions();
        CacheBones();
    }

    #region setup

    /// <summary>
    /// Sets up the limbs of the creature
    /// </summary>
    private void LimbSetup()
    {
        Bone[] limbRoots = GetDirectChildBones(limbBase);

        foreach (Bone limbStart in limbRoots)
        {
            _limbs.Add(BuildLimb(limbStart));
        }
    }

    /// <summary>
    /// Creates the creature's bones
    /// </summary>
    private void CreateBones()
    {
        Vector2 legDirection = (legAmount % 2 == 0) ? Vector2.right : Vector2.up;

        for (int i = 0; i < legAmount; i++)
        {
            Transform boneToAttachTo = limbBase.transform;

            for (int j = 0; j < 3; j++)
            {
                string boneName = "Undefined Bone";

                switch (j)
                {
                    case 0:
                        boneName = "Hip";
                        break;
                    case 1:
                        boneName = "Knee";
                        break;
                    case 2:
                        boneName = "Foot";
                        break;
                }

                GameObject newBone = new GameObject($"{boneName} {j + 1}", typeof(Bone));
                newBone.transform.position = limbBase.transform.position + (Vector3)legDirection * (legLength / 2) * j;
                newBone.transform.SetParent(boneToAttachTo, true);

                boneToAttachTo = newBone.transform;
            }

            legDirection = Quaternion.AngleAxis(360 / legAmount, Vector3.forward) * legDirection;
        }
    }

    /// <summary>
    /// Builds a limb from every children of the selected bone
    /// </summary>
    /// <param name="limbStart">First bone of the limb</param>
    /// <returns></returns>
    private Limb BuildLimb(Bone limbStart)
    {
        List<Bone> limbBones = new();

        limbBones.Add(limbStart);

        GetAllChildBones(limbStart.transform, limbBones);

        Limb newLimb = new Limb(limbBones.ToArray());

        CreateSolver(newLimb);

        return newLimb;
    }

    /// <summary>
    /// Creates the given limb's IK Solver and its associated target
    /// </summary>
    /// <param name="limb"></param>
    private void CreateSolver(Limb limb)
    {
        GameObject newSolver = new GameObject("Solver Test");
        newSolver.transform.parent = solversParent;
        limb.solver = newSolver.AddComponent<LimbSolver2D>();

        IKChain2D chain = limb.solver.GetChain(0);

        GameObject newTarget = new GameObject("Target Test");
        newTarget.transform.position = limb.endBone.transform.position;
        newTarget.transform.SetParent(targetsParent, true);
        chain.target = newTarget.transform;

        chain.effector = limb.endBone.transform;

        _IKManager.AddSolver(limb.solver);

        //limb.solver.Initialize();
        //limb.solver.UpdateIK(1f);
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

    private void CacheBones()
    {
        Bone[] bones = limbBase.GetComponentsInChildren<Bone>();
    }

    #endregion

    private void GetIdealLegPositions()
    {
        foreach (Limb limb in _limbs)
        {
            _idealLegPositions.Add(limb.offsetFromRoot * legBaseDistance);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;

        Gizmos.color = Color.yellow;

        foreach (Vector3 position in _idealLegPositions)
        {
            Gizmos.DrawSphere(limbBase.transform.position + position, 0.5f);
        }

        Gizmos.color = Color.blue;

        foreach (Bone b in bones)
        {
            Gizmos.DrawSphere(b.transform.position, 0.5f);
        }
    }
}
