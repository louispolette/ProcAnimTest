using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;

public class SpiderLimbScript : MonoBehaviour
{
    #region serialized

    [Space]

    [Tooltip("Shared parent of all limbs")]
    public Bone limbBase;

    [Space]

    [SerializeField] private int legAmount;

    [SerializeField] private float legLength;

    [Tooltip("How much should the spider extend its leg when no surface is available, 1 is fully extended")]
    [SerializeField, Range(0f, 1f)] private float footDefaultDistance = 0.75f;

    [SerializeField] private float stepDuration = 0.1f;

    [SerializeField] private LayerMask legLayerMask;

    [Space]

    [Tooltip("Parent that will contain all IK Solvers")]
    [SerializeField] private Transform solversParent;

    [Tooltip("Parent that will contain all IK Targets")]
    [SerializeField] private Transform targetsParent;

    [Tooltip("Parent that will contain all the legs line renderers")]
    [SerializeField] public Transform rendererParent { get; private set; }

    [Space]

    [SerializeField] LineRenderer lineRendererPrefab;

    [Space]

    [SerializeField] private SolverParameters solverParameters;

    [Serializable]
    private class SolverParameters
    {
        public bool solveFromDefaultPose = true;
        public bool constrainRotation = false;
        public bool flip = false;
    }

    [Header("Debug")]

    [SerializeField] private bool _enableDebug = false;

    [SerializeField] private debugSettings _debugSettings;

    [Serializable]
    private struct debugSettings
    {
        public bool bones;
        public bool raycasts;
        public bool defaultPositions;
        public bool lerpPositions;
        public bool targetPositions;
    }

    #endregion

    #region not serialized

    public List<Limb> _limbs { get; private set; } = new List<Limb>();

    private IKManager2D _IKManager;

    private Bone[] bones;

    public delegate void OnLimbsSetupDone();
    public event OnLimbsSetupDone onLimbsSetupDone;

    #endregion

    private void Awake()
    {
        _IKManager = GetComponentInChildren<IKManager2D>();
    }

    private void Start()
    {
        CreateBones();
        LimbSetup();
        CacheBones();

        onLimbsSetupDone?.Invoke();
    }

    private void Update()
    {
        foreach (Limb limb in _limbs)
        {
            Ray2D ray = new Ray2D(limb.startBone.transform.position, limb.direction);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, limb.length, legLayerMask);

            bool targetPositionIsGrounded = false;

            if (hit)
            {
                targetPositionIsGrounded = true;
                limb.targetPosition = hit.point;

                if (!limb.isOnGround) MoveLimb(limb, true);
            }
            else
            {
                limb.targetPosition = ray.GetPoint(limb.length * limb.defaultDistance);
            }

            if (_enableDebug && _debugSettings.raycasts) Debug.DrawLine(ray.origin, ray.GetPoint(limb.length), Color.magenta);

            if (Vector2.Distance(limb.startBone.transform.position, limb.lerpPosition) > limb.length)
            {
                MoveLimb(limb, targetPositionIsGrounded);
            }
            
            if (!limb.isStepping)
            {
                limb.IKTarget.transform.position = limb.lerpPosition;
            };
        }
    }

    private void MoveLimb(Limb limb, bool targetPositionIsGrounded = false)
    {
        limb.MoveLerpPosition(targetPositionIsGrounded);

        if (limb.stepCoroutine != null)
        {
            StopCoroutine(limb.stepCoroutine);
        }

        limb.stepCoroutine = StartCoroutine(limb.Step(stepDuration));
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

                GameObject newBone = new GameObject($"{boneName} {i + 1}", typeof(Bone));

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
        CreateRenderer(newLimb);

        newLimb.direction = (newLimb.endBone.transform.position - newLimb.startBone.transform.position).normalized;
        newLimb.defaultDistance = footDefaultDistance;

        return newLimb;
    }

    /// <summary>
    /// Creates the given limb's IK Solver and its associated target
    /// </summary>
    /// <param name="limb"></param>
    private void CreateSolver(Limb limb)
    {
        // Solver

        GameObject newSolver = new GameObject("Solver");
        newSolver.transform.parent = solversParent;
        limb.solver = newSolver.AddComponent<LimbSolver2D>();

        limb.solver.solveFromDefaultPose = solverParameters.solveFromDefaultPose;
        limb.solver.constrainRotation = solverParameters.constrainRotation;
        limb.solver.flip = solverParameters.flip;

        // Target

        IKChain2D chain = limb.solver.GetChain(0);

        GameObject newTarget = new GameObject("Target");
        newTarget.transform.position = limb.endBone.transform.position;
        newTarget.transform.SetParent(targetsParent, true);
        chain.target = newTarget.transform;
        limb.IKTarget = newTarget.transform;

        // Effector

        chain.effector = limb.endBone.transform;

        // Add to manager

        _IKManager.AddSolver(limb.solver);
    }

    /// <summary>
    /// Creates the line renderer for the given limb
    /// </summary>
    /// <param name="limb"></param>
    private void CreateRenderer(Limb limb)
    {
        LineRenderer newRenderer = Instantiate(lineRendererPrefab, rendererParent);
        limb.renderer = newRenderer;
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

    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;

        foreach (Limb limb in _limbs)
        {
            if (_debugSettings.targetPositions)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(limb.targetPosition, 0.2f);
            }
            
            if (_debugSettings.lerpPositions)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(limb.lerpPosition, 0.1f);
            }
            
            if (_debugSettings.defaultPositions)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere((Vector2)limb.startBone.transform.position + limb.direction * limb.length * limb.defaultDistance, 0.25f);
            }
        }

        if (!_debugSettings.bones) return;

        Gizmos.color = Color.blue;

        Bone[] allBones = limbBase.GetComponentsInChildren<Bone>();

        foreach (Bone b in allBones)
        {
            Gizmos.DrawSphere(b.transform.position, 0.20f);
        }
    }
}
