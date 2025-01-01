using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;
using UnityEngine.U2D.IK;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class SpiderLimbHandler : MonoBehaviour
{
    #region serialized

    [Space]

    [Header("Limb Placement")]

    [SerializeField] private float legLength;

    [Tooltip("Limbs will reposition themselves if their foot is too close to the body")]
    [SerializeField] private float footSpacingFromBase = 2f;

    [Space]

    [Tooltip("Wether to allow feet to \"float\" when no solid ground is available to them")]
    [SerializeField] private bool allowFloatingFeet = false;

    [Tooltip("How much should the spider extend its leg when no surface is available and are allow to have floating feet\n\n 1 is fully extended")]
    [SerializeField, Range(0f, 1f)] private float footFloatingDistance = 0.75f;

    [Tooltip("Makes legs place themselvses slightly nearer/farther than their default position when no surface is available " +
        "to avoid making a clear ring of feet.\n\n0 = disable\n1 = full possible range" +
        "\n\nThe range depends on footSpacingFromBase and legLength")]
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

    [Header("Audio")]

    [SerializeField] private AudioClip[] _stepSFX;

    [Header("Debug")]

    [SerializeField] private bool _enableDebug = false;

    [SerializeField] private DebugSettings _debugSettings;

    [Serializable]
    private struct DebugSettings
    {
        public bool bones;
        public bool raycasts;
        public bool footSpacingFromBase;
        public bool legLength;
        public bool floatingPositions;
        public bool footPlacementRange;
        public bool lerpPositions;
        public bool targetPositions;
        public bool averageLimbPosition;
    }

    #endregion

    #region not serialized

    public List<Limb> Limbs { get; private set; } = new List<Limb>();

    private IKManager2D _IKManager;
    private AudioSource _audioSource;

    private Bone[] _bones;

    public delegate void OnLimbsSetupDone();
    public event OnLimbsSetupDone onLimbsSetupDone;

    private List<Limb> _searchList = new List<Limb>();
    private List<Limb> _validLimbsList = new List<Limb>();
    private List<Vector2> _limbPositionsTemp = new List<Vector2>();

    #endregion

    #region

    public enum LimbStepContext
    {
        None,
        TooFar,
        TooClose,
        Clipping,
        FoundBetterPosition,
        ForcedToMove,
        Unspecified
    }

    #endregion

    private void Awake()
    {
        _IKManager = GetComponentInChildren<IKManager2D>();
        _audioSource = GetComponentInChildren<AudioSource>();

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

    private void FixedUpdate()
    {
        foreach (Limb limb in Limbs)
        {
            if (CheckIfHipInWall()) continue;

            bool foundtargetPosition;
            bool foundTargetPositionIsGrounded; // Wether the found target position is grounded or not
            bool limbFoundBetterPosition = false; // If false, the limb was either using another limb's position or was floating and found available ground
            
            bool kneeIsClipping; // Wether the limb's knee bone is clipping in a wall this frame or not
            bool limbHasToRelocate = false; // True when the limb needs to relocate beacause it's knee is in a wall and cannot flip to fix it

            bool limbTooFar;
            bool limbTooClose;
            bool limbForcedToMove;

            Ray2D ray;
            RaycastHit2D hit;

            DoRaycast();

            foundtargetPosition = FindTargetPosition();
            kneeIsClipping = CheckIfKneeInWall();

            if (!foundtargetPosition || kneeIsClipping)
            {
                limb.HasValidPosition = false; // The limb is either clipping into a wall or couldn't find a target position and couldn't use another limb's position
            }

            limbTooFar = CheckIfTooFar();
            limbTooClose = CheckIfTooClose();

            limbForcedToMove = limb.IsForcedToMove;
            limb.IsForcedToMove = false;

            LimbStepContext stepContext = GetStepContext();

            if (stepContext != LimbStepContext.None)
            {
                MoveLimb(limb, foundTargetPositionIsGrounded, stepContext);
            }

            #region local functions

            bool FindTargetPosition()
            {
                foundTargetPositionIsGrounded = false;

                if (hit) // The limb has found ground to attach to
                {
                    if (!limb.LerpPositionIsGrounded || !limb.IsInIdealPosition) // Immediatly move to the found position if limb is not in ideal position
                    {
                        limbFoundBetterPosition = true;
                    }

                    foundTargetPositionIsGrounded = true;
                    limb.TargetPosition = hit.point;
                    limb.IsInIdealPosition = true;
                }
                else if (allowFloatingFeet) // No ground found but we're allowed to have floating feet
                {
                    float targetDistance = GetFloatingFootTargetDistance(limb);

                    limb.TargetPosition = ray.GetPoint(targetDistance);
                    limb.IsInIdealPosition = true;
                }
                else // We try to attach to another limb's position
                {
                    limb.IsInIdealPosition = false;

                    Limb foundLimb = SearchValidNeighborLimb(limb); // Tries to find a limb with a valid position so it can copy its target position

                    if (foundLimb != null)
                    {
                        limb.TargetPosition = foundLimb.TargetPosition;
                        foundTargetPositionIsGrounded = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }

            bool CheckIfHipInWall()
            {
                if (limb.IsHipInWall(legLayerMask))
                {
                    limb.HasValidPosition = false; // Limb is immediatly considered invalid if the hip is in a wall
                    return true;
                }
                else
                {
                    limb.HasValidPosition = true;
                    return false;
                }
            }

            bool CheckIfKneeInWall()
            {
                if (limb.IsKneeInWall(legLayerMask) && !limb.IsStepping) // Flip elbow if it is in a wall
                {
                    FlipLimb(limb);
                    limb.Solver.UpdateIK(1f);

                    if (limb.IsKneeInWall(legLayerMask)) // Check again after flip
                    {
                        FlipLimb(limb); // Flip back

                        Limb foundLimb = SearchValidNeighborLimb(limb);

                        if (foundLimb != null)
                        {
                            // FIND OUT WHY THE LEGS DON'T RELOCATE WHEN KNEES ARE CLIPPING

                            limb.TargetPosition = foundLimb.TargetPosition;
                            foundTargetPositionIsGrounded = foundLimb.LerpPositionIsGrounded;
                            limbHasToRelocate = true;

                            limb.IsInIdealPosition = false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool CheckIfTooFar()
            {
                return Vector2.Distance(limb.HipBone.transform.position, limb.LerpPosition) > limb.Length;
            }

            bool CheckIfTooClose()
            {
                return limb.IsFloating && Vector2.Distance(limb.LerpPosition, limbBase.transform.position) < footSpacingFromBase;
            }

            void DoRaycast()
            {
                ray = new Ray2D(limb.HipBone.transform.position, limb.Direction);
                hit = Physics2D.Raycast(ray.origin, ray.direction, limb.Length, legLayerMask);
                DrawRaycast();
            }

            void DrawRaycast()
            {
                if (_enableDebug && _debugSettings.raycasts)
                {
                    Debug.DrawLine(ray.origin, ray.GetPoint(limb.Length), Color.magenta);
                }
            }

            LimbStepContext GetStepContext()
            {
                // The limb will move under one of the following conditions :

                if (limbTooFar) // The limb cannot extend further
                {
                    return LimbStepContext.TooFar;
                }
                if (limbForcedToMove)
                {
                    return LimbStepContext.ForcedToMove;
                }
                else if (limbTooClose) // The limb's lerp position is too close to the body, only if the limb is floating
                {
                    return LimbStepContext.TooClose;
                }
                else if (limbHasToRelocate) // The limb's knee is stuck in a wall and is forced to take another leg's position
                {
                    return LimbStepContext.Clipping;
                }
                else if (limbFoundBetterPosition) // The limb has found a better position than it's current one
                {
                    return LimbStepContext.FoundBetterPosition;
                }

                return LimbStepContext.None;
            }

            #endregion
        }
    }

    private void LateUpdate()
    {
        foreach (Limb limb in Limbs)
        {
            // Keep foot in position when the limb isn't stepping :

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
    private void MoveLimb(Limb limb, bool targetPositionIsGrounded, LimbStepContext context = LimbStepContext.Unspecified)
    {
        limb.MoveLerpPosition(targetPositionIsGrounded);

        if (limb.IsStepping && context != LimbStepContext.TooFar) return; // Stop here if a step is happening and we're not allowed to cancel it

        if (limb.StepCoroutine != null)
        {
            StopCoroutine(limb.StepCoroutine);
        }

        limb.StepCoroutine = StartCoroutine(limb.Step(stepDuration));
    }

    public void MoveLimb(Limb limb)
    {

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

    private float GetFloatingFootTargetDistance(Limb limb)
    {
        float baseDist = Mathf.Lerp(limb.SpacingFromBase, limb.Length, footFloatingDistance);
        float randomizedDist = baseDist + Random.Range(limb.SpacingFromBase - baseDist, limb.Length - baseDist) * footPlacementRange;

        return randomizedDist;
    }

    /// <summary>
    /// Searches for another limb that has a valid target position.
    /// Prioritizes limbs that are the closest to the given limb
    /// </summary>
    /// <param name="limb">The limb from which to start searching from</param>
    /// <returns>The limb that has been found</returns>
    private Limb SearchValidNeighborLimb(Limb limb)
    {
        const int iterationLimit = 100; // Prevents crash
        int iterations = 0;

        _searchList.Clear();
        _searchList.Add(limb);
        _validLimbsList.Clear();

        // We search in 2 directions, clockwise & counterclockwise :

        int clockwiseLimbID = limb.ID + 1;
        int counterClockwiseLimbID = limb.ID - 1;

        bool goClockwise = false; // This lets us alternate between clockwise & counter clockwise search

        // These let us know when a search direction has encountered a limb that has already been seen :

        bool clockwiseHasLooped = false;
        bool counterClockwiseHasLooped = false;

        Limb neighborLimb = null; // The limb that we are currently inspecting
        bool limbsFound = false; // Wether we found at least one valid limb or not

        while (!(clockwiseHasLooped && counterClockwiseHasLooped)) // If both searches have looped without finding a valid limb, then we end the while() loop
        {
            iterations++;

            if (iterations > iterationLimit)
            {
                Debug.LogError("Iteration limit reached !");
                break;
            }

            goClockwise = !goClockwise; // Switch search direction
            neighborLimb = null;

            if (goClockwise && !clockwiseHasLooped)
            {
                if (clockwiseLimbID >= Limbs.Count) // Index out of range
                {
                    clockwiseLimbID = 0; // Loop
                }

                neighborLimb = Limbs[clockwiseLimbID];
                clockwiseLimbID++;
            }
            else if (!goClockwise && !counterClockwiseHasLooped)
            {
                if (counterClockwiseLimbID < 0) // Index out range
                {
                    counterClockwiseLimbID = Limbs.Count - 1; // Loop
                }

                neighborLimb = Limbs[counterClockwiseLimbID];
                counterClockwiseLimbID--;
            }

            if (neighborLimb == null) continue; // neighborLimb being null means that one or both of the search directions have looped

            if (_searchList.Contains(neighborLimb)) // Check if we haven't already seen this limb, if we did, then one of the searching directions has looped
            {
                if (goClockwise)
                {
                    clockwiseHasLooped = true;
                }
                else
                {
                    counterClockwiseHasLooped = true;
                }
            }
            else if (neighborLimb.HasValidPosition) // If we haven't seen it, we check if it is valid
            {
                limbsFound = true;
                _validLimbsList.Add(neighborLimb);
                _searchList.Add(neighborLimb);
            }
            else // If it isn't valid, we put in the list of limbs we've checked
            {
                _searchList.Add(neighborLimb);
            }
        }

        Limb chosenLimb = null;

        if (limbsFound)
        {
            chosenLimb = _validLimbsList[Random.Range(0, _validLimbsList.Count - 1)]; // Get random valid limb
        }

        return chosenLimb;
    }

    #region setup

    /// <summary>
    /// Sets up the limbs of the creature
    /// </summary>
    private void LimbSetup()
    {
        Bone[] limbRoots = GetDirectChildBones(limbBase);

        for (int i = 0; i < limbRoots.Length; i++)
        {
            Limbs.Add(BuildLimb(limbRoots[i], i)) ;
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
    private Limb BuildLimb(Bone limbStart, int limbID)
    {
        List<Bone> limbBones = new();

        limbBones.Add(limbStart);

        GetAllChildBones(limbStart.transform, limbBones);

        Limb newLimb = new Limb(limbBones.ToArray(), limbID);

        CreateSolver(newLimb);
        CreateRenderer(newLimb);

        newLimb.SpacingFromBase = this.footSpacingFromBase;
        newLimb.FloatingDistance = this.footFloatingDistance;

        SetupLimbCallbacks(newLimb);

        return newLimb;
    }

    private void SetupLimbCallbacks(Limb limb)
    {
        limb.onStepEnd += PlayStepSFX;
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

    private void PlayStepSFX()
    {
        int randomSFXindex = Random.Range(0, _stepSFX.Length);
        _audioSource.PlayOneShot(_stepSFX[randomSFXindex]);
    }

    public Vector2[] GetLimbPositions()
    {
        _limbPositionsTemp.Clear();

        foreach (Limb limb in Limbs)
        {
            _limbPositionsTemp.Add(limb.FootBone.transform.position);
        }

        return _limbPositionsTemp.ToArray();
    }

    public Vector2 GetAverageLimbPosition()
    {
        Vector2[] limbPositions = GetLimbPositions();
        Vector2 positionSum = new Vector2();

        foreach (Vector2 position in limbPositions)
        {
            positionSum += position;
        }

        return positionSum / limbPositions.Length;
    }

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

        foreach (Limb limb in Limbs)
        {
            if (_debugSettings.targetPositions)
            {
                Gizmos.color = Color.red;

                if (limb.LerpPositionIsGrounded)
                {
                    Gizmos.DrawSphere(limb.TargetPosition, 0.4f);
                }
                else if (allowFloatingFeet)
                {
                    float floatingDist = Mathf.Lerp(footSpacingFromBase, legLength, footFloatingDistance);
                    Gizmos.DrawLine((Vector2)limbBase.transform.position + limb.Direction * Mathf.Lerp(floatingDist, footSpacingFromBase, footPlacementRange),
                                    (Vector2)limbBase.transform.position + limb.Direction * Mathf.Lerp(floatingDist, legLength, footPlacementRange));
                }
            }

            if (_debugSettings.lerpPositions)
            {
                Gizmos.color = (!limb.IsFloating) ? Color.yellow : Color.grey;
                Gizmos.DrawSphere(limb.LerpPosition, (!limb.IsFloating) ? 0.15f : 0.3f);
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

        if (_debugSettings.averageLimbPosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(GetAverageLimbPosition(), 1f);
        }
    }
}
