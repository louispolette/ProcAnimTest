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
    [SerializeField] private float footSpacingFromBase = 2f;

    [Space]

    [Tooltip("How much should the spider extend its leg when no surface is available, 1 is fully extended")]
    [SerializeField, Range(0f, 1f)] private float footFloatingDistance = 0.75f;

    [Tooltip("Makes legs place themselvses slightly nearer/farther than their default position when no surface is available " +
        "to avoid making a clear ring of feet. 0 to disable and 1 to have the full possible range " +
        "(the range depends on footSpacingFromBase and legLength)")]
    [SerializeField, Range(0f, 1f)] private float footPlacementRange = 0.5f;

    [Space]

    [Tooltip("Duration in seconds of a step")]
    [SerializeField] private float stepDuration = 0.1f;

    [Tooltip("Duration in seconds of a knee flipping positions")]
    [SerializeField] private float kneeFlipDuration = 0.1f;

    [Tooltip("Duration in seconds of a limb retracting when no valid position is found")]
    [SerializeField] public float retractDuration = 0.1f;

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
        public bool footSpacingFromBase;
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

        if (legLength <= footSpacingFromBase)
        {
            Debug.LogWarning("legLength is lower or equal to footSpacingFromBase, this may cause issues");
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
            Ray2D ray = new Ray2D(limb.HipBone.transform.position, limb.Direction);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, limb.Length, legLayerMask);

            bool targetPositionIsGrounded = false;

            if (hit)
            {
                targetPositionIsGrounded = true;
                limb.TargetPosition = hit.point;

                if (!limb.HasAvailableGround)
                {
                    MoveLimb(limb, true);
                    continue;
                }
            }
            else
            {
                float baseDist = Mathf.Lerp(limb.SpacingFromBase, limb.Length, footFloatingDistance);
                float randomizedDist = baseDist + Random.Range(limb.SpacingFromBase - baseDist, limb.Length - baseDist) * footPlacementRange;
                limb.TargetPosition = ray.GetPoint(randomizedDist);
            }

            if (_enableDebug && _debugSettings.raycasts) Debug.DrawLine(ray.origin, ray.GetPoint(limb.Length), Color.magenta);

            // The limb will move under one of the 2 following conditions :

            // The limb cannot extend further
            bool limbOverExtended = Vector2.Distance(limb.HipBone.transform.position, limb.LerpPosition) > limb.Length;
            // The limb's lerp position is too close to the body (ignored if the limb is attached to ground)
            bool limbTooClose = !limb.HasAvailableGround && Vector2.Distance(limb.LerpPosition, limbBase.transform.position) < footSpacingFromBase;

            if (limbOverExtended)
            {
                MoveLimb(limb, targetPositionIsGrounded, allowStepCancel : true);
            }
            else if (limbTooClose)  
            {
                MoveLimb(limb, targetPositionIsGrounded, allowStepCancel : false);
            }

            if (limb.IsKneeInWall(legLayerMask)) // Flip elbow if it is in a wall
            {
                FlipLimb(limb);
                limb.Solver.UpdateIK(1f);

                if (limb.IsKneeInWall(legLayerMask)) // Check again after flip and retract if also in wall
                {
                    FlipLimb(limb);

                    limb.IsForcedIntoWall = true;
                    limb.IsRetracted = true;
                }
            }
            else
            {
                limb.IsForcedIntoWall = false;
                limb.IsRetracted = false;
            }

            // Keep foot in position when the body is moving and limb isn't stepping :

            if (!limb.IsStepping) 
            {
                limb.IKTarget.transform.position = limb.LerpPosition;
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

        if (limb.IsStepping && !allowStepCancel) return; // Stop here if a step is happening and we're not allowed to cancel it

        if (limb.StepCoroutine != null)
        {
            StopCoroutine(limb.StepCoroutine);
        }

        limb.StepCoroutine = StartCoroutine(limb.Step(stepDuration));
    }

    /// <summary>
    /// Flips the knee position of a limb
    /// </summary>
    /// <param name="limb">The limb to flip</param>
    private void FlipLimb(Limb limb)
    {
        limb.Solver.flip = !limb.Solver.flip;

        if (limb.FlipCoroutine != null)
        {
            StopCoroutine(limb.FlipCoroutine);
        }

        limb.FlipCoroutine = StartCoroutine(limb.FlipKnee(kneeFlipDuration));
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

        newLimb.SpacingFromBase = this.footSpacingFromBase;
        newLimb.FloatingDistance = this.footFloatingDistance;

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
        limb.Solver = newSolver.AddComponent<LimbSolver2D>();

        limb.Solver.solveFromDefaultPose = solverParameters.solveFromDefaultPose;
        limb.Solver.constrainRotation = solverParameters.constrainRotation;
        limb.Solver.flip = solverParameters.flip;

        // Target

        IKChain2D chain = limb.Solver.GetChain(0);

        GameObject newTarget = new GameObject("Target");
        newTarget.transform.position = limb.FootBone.transform.position;
        newTarget.transform.SetParent(targetsParent, true);
        chain.target = newTarget.transform;
        limb.IKTarget = newTarget.transform;

        // Effector

        chain.effector = limb.FootBone.transform;

        // Add to manager

        _IKManager.AddSolver(limb.Solver);
    }

    /// <summary>
    /// Creates the line renderer for the given limb
    /// </summary>
    /// <param name="limb"></param>
    private void CreateRenderer(Limb limb)
    {
        LineRenderer newRenderer = Instantiate(lineRendererPrefab, rendererParent);
        limb.Renderer = newRenderer;
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

        if (_debugSettings.footSpacingFromBase)
        {
            if (footFloatingDistance != 0)
            {
                Gizmos.color = (footPlacementRange != 0) ? Color.white : Color.gray;
                Gizmos.DrawWireSphere(limbBase.transform.position, footSpacingFromBase);
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
                Gizmos.DrawWireSphere(limbBase.transform.position, Mathf.Lerp(footSpacingFromBase, legLength, footFloatingDistance));
            }
        }

        if (_debugSettings.footPlacementRange)
        {
            Gizmos.color = Color.black;
            float floatingDist = Mathf.Lerp(footSpacingFromBase, legLength, footFloatingDistance);
            if (footPlacementRange != 1 && footPlacementRange != 0)
            {
                if (footFloatingDistance != 0)
                {
                    Gizmos.DrawWireSphere(limbBase.transform.position, Mathf.Lerp(floatingDist, footSpacingFromBase, footPlacementRange));
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

                if (limb.HasAvailableGround)
                {
                    Gizmos.DrawSphere(limb.TargetPosition, 0.2f);
                }
                else
                {
                    float floatingDist = Mathf.Lerp(footSpacingFromBase, legLength, footFloatingDistance);
                    Gizmos.DrawLine((Vector2)limbBase.transform.position + limb.Direction * Mathf.Lerp(floatingDist, footSpacingFromBase, footPlacementRange),
                                    (Vector2)limbBase.transform.position + limb.Direction * Mathf.Lerp(floatingDist, legLength, footPlacementRange));
                }
            }

            if (_debugSettings.lerpPositions)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(limb.LerpPosition, 0.15f);
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
