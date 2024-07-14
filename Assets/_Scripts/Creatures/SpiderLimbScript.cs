using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.IK;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class SpiderLimbScript : MonoBehaviour
{
    #region serialized

    [Space]

    [Header("Limb Placement")]

    [SerializeField] private float legLength;

    [Tooltip("Limbs will reposition themselves if their foot is too close to the body")]
    [SerializeField] private float footSpacingFromBody = 2f;

    [Space]

    [Tooltip("How much should the spider extend its leg when no surface is available, 1 is fully extended")]
    [SerializeField, Range(0f, 1f)] private float footFloatingDistance = 0.75f;

    [Tooltip("Makes legs place themselvses slightly nearer/farther than their default position when no surface is available " +
        "to avoid making a clear ring of feet. 0 to disable and 1 to have the full possible range " +
        "(the range depends on footSpacingFromBody and legLength)")]
    [SerializeField, Range(0f, 1f)] private float footPlacementRange = 0.5f;

    [Space]

    [Tooltip("Duration in seconds of a step")]
    [SerializeField] private float stepDuration = 0.1f;

    [Tooltip("Duration in seconds of a limb retracting when no valid position is found")]
    [SerializeField] public float retractDuration = 0.25f;

    [Space]

    [Tooltip("The layers that the legs will attach to")]
    [SerializeField] private LayerMask legLayerMask;

    [Header("Limb Generation")]

    [Tooltip("Shared parent of all limbs")]
    public Bone limbBase;

    [Space]

    [SerializeField, Min(0)] private int legAmount;

    [Space]

    [Tooltip("Parent that will contain all IK Solvers")]
    [SerializeField] private Transform solversParent;

    [Tooltip("Parent that will contain all IK Targets")]
    [SerializeField] private Transform targetsParent;

    [Tooltip("Parent that will contain all the legs line renderers")]
    [SerializeField] private Transform rendererParent;

    [Space]

    [Tooltip("Renderer used to draw the legs")]
    [SerializeField] LineRenderer lineRendererPrefab;

    [Space]

    [Tooltip("The parameters of the generated limb solvers")]
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
        public bool footBodySpacing;
        public bool legLength;
        public bool floatingPositions;
        public bool footPlacementRange;
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

        #region warning logs

        if (legLength <= footSpacingFromBody)
        {
            Debug.LogWarning("legLength is lower or equal to footSpacingFromBody, this may cause issues");
        }

        #endregion
    }

    private void Start()
    {
        CreateBones();
        LimbSetup();
        CacheBones();

        onLimbsSetupDone?.Invoke();
    }

    #region movement

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

                if (!limb.hasAvailableGround)
                {
                    MoveLimb(limb, true);
                    continue;
                }
            }
            else
            {
                float dist = Mathf.Lerp(footSpacingFromBody, limb.length, footFloatingDistance);
                float randomDist = dist + Random.Range(footSpacingFromBody - dist, limb.length - dist) * footPlacementRange;
                limb.targetPosition = ray.GetPoint(randomDist);
            }

            if (_enableDebug && _debugSettings.raycasts) Debug.DrawLine(ray.origin, ray.GetPoint(limb.length), Color.magenta);

            // The limb will move under one of the 2 following conditions :

            // The limb cannot extend further
            bool limbOverExtended = Vector2.Distance(limb.startBone.transform.position, limb.lerpPosition) > limb.length;
            // The limb's lerp position is too close to the body (ignored if the limb is attached to ground)
            bool limbTooClose = !limb.hasAvailableGround && Vector2.Distance(limb.lerpPosition, limbBase.transform.position) < footSpacingFromBody;

            if (limbOverExtended)
            {
                MoveLimb(limb, targetPositionIsGrounded, allowStepCancel : true);
            }
            else if (limbTooClose)  
            {
                MoveLimb(limb, targetPositionIsGrounded, allowStepCancel : false);
            }

            if (limb.IsElbowIsInWall(legLayerMask))
            {
                limb.solver.flip = !limb.solver.flip;
                limb.solver.UpdateIK(1f);

                if (limb.IsElbowIsInWall(legLayerMask))
                {
                    limb.solver.flip = !limb.solver.flip;

                    limb.isForcedIntoWall = true;
                    limb.isRetracted = true;
                }
            }
            else
            {
                limb.isForcedIntoWall = false;
                limb.isRetracted = false;
            }

            // Keep foot in position when the body is moving and limb isn't stepping :

            if (!limb.isStepping) 
            {
                limb.IKTarget.transform.position = limb.lerpPosition;
            }
        }
    }

    

    /// <summary>
    /// Makes the limb take a step
    /// </summary>
    /// <param name="limb">The limb that will take a step</param>
    /// <param name="targetPositionIsGrounded">Wether the current target position is grounded or not</param>
    /// <param name="allowStepCancel">Wether the limb is allowed to cancel a step if one is already happening or not</param>
    private void MoveLimb(Limb limb, bool targetPositionIsGrounded = false, bool allowStepCancel = false)
    {
        limb.MoveLerpPosition(targetPositionIsGrounded);

        if (limb.isStepping && !allowStepCancel) return; // Stop here if a step is happening and we're not allowed to cancel it

        if (limb.stepCoroutine != null)
        {
            StopCoroutine(limb.stepCoroutine);
        }

        limb.stepCoroutine = StartCoroutine(limb.Step(stepDuration));
    }

    #endregion

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
        newLimb.floatingDistance = footFloatingDistance;

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

        if (_debugSettings.footBodySpacing)
        {
            if (footFloatingDistance != 0)
            {
                Gizmos.color = (footPlacementRange != 0) ? Color.white : Color.gray;
                Gizmos.DrawWireSphere(limbBase.transform.position, footSpacingFromBody);
            }
        }

        if (_debugSettings.legLength)
        {
            if (footFloatingDistance != 1)
            {
                Gizmos.color = (footPlacementRange != 1) ? Color.white : Color.gray;
                Gizmos.DrawWireSphere(limbBase.transform.position, legLength);
            }
        }

        if (_debugSettings.floatingPositions)
        {
            if (footPlacementRange != 1)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(limbBase.transform.position, Mathf.Lerp(footSpacingFromBody, legLength, footFloatingDistance));
            }
        }

        if (_debugSettings.footPlacementRange)
        {
            Gizmos.color = Color.black;
            float floatingDist = Mathf.Lerp(footSpacingFromBody, legLength, footFloatingDistance);
            if (footPlacementRange != 1 && footPlacementRange != 0)
            {
                if (footFloatingDistance != 0)
                {
                    Gizmos.DrawWireSphere(limbBase.transform.position, Mathf.Lerp(floatingDist, footSpacingFromBody, footPlacementRange));
                }
                if (footFloatingDistance != 1)
                {
                    Gizmos.DrawWireSphere(limbBase.transform.position, Mathf.Lerp(floatingDist, legLength, footPlacementRange));
                }
            }
        }

        foreach (Limb limb in _limbs)
        {
            if (_debugSettings.targetPositions)
            {
                Gizmos.color = Color.red;

                if (limb.hasAvailableGround)
                {
                    Gizmos.DrawSphere(limb.targetPosition, 0.2f);
                }
                else
                {
                    float floatingDist = Mathf.Lerp(footSpacingFromBody, legLength, footFloatingDistance);
                    Gizmos.DrawLine((Vector2)limbBase.transform.position + limb.direction * Mathf.Lerp(floatingDist, footSpacingFromBody, footPlacementRange),
                                    (Vector2)limbBase.transform.position + limb.direction * Mathf.Lerp(floatingDist, legLength, footPlacementRange));
                }
            }

            if (_debugSettings.lerpPositions)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(limb.lerpPosition, 0.15f);
            }
        }

        if (_debugSettings.bones)
        {
            Gizmos.color = Color.blue;

            Bone[] allBones = limbBase.GetComponentsInChildren<Bone>();

            foreach (Bone b in allBones)
            {
                Gizmos.DrawSphere(b.transform.position, 0.2f);
            }
        }
    }
}
