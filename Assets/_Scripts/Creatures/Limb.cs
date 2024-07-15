using System;
using System.Collections;
using UnityEngine;
using UnityEngine.U2D.IK;

[Serializable]
public class Limb
{
    #region members
    /// <summary>
    /// The 3 bones that that make the limb, from hip bone to foot bone
    /// </summary>
    public Bone[] Bones { get; }
    public Bone HipBone { get; }
    public Bone FootBone { get; }
    public Bone KneeBone { get; }

    /// <summary>
    /// The LineRenderer component that draws connections between the limb's bones
    /// </summary>
    public LineRenderer Renderer { get; set; }
    /// <summary>
    /// The component that handles inverse kinematics of the limb
    /// </summary>
    public LimbSolver2D Solver { get; set; }
    /// <summary>
    /// The transform that the IK solver uses to determine bone positions
    /// </summary>
    public Transform IKTarget { get; set; }

    /// <summary>
    /// Length of the limb when fully extended
    /// </summary>
    public float Length { get; }
    /// <summary>
    /// Distance between the limb's hip bone and foot bone where the foot will take a step
    /// </summary>
    public float SpacingFromBase { get; set; }
    public Vector2 Direction { get; }
    /// <summary>
    /// Lerped position in between the limb's length and it's spacing from the base that is used when the limb has no grounded position to step to
    /// </summary>
    public float FloatingDistance { get; set; }

    /// <summary>
    /// The position that the limb will move to when it is asked to take a step
    /// </summary>
    public Vector2 TargetPosition { get; set; }
    /// <summary>
    /// The position that the limb's foot is continually moving towards
    /// </summary>
    public Vector2 LerpPosition { get; set; }

    /// <summary>
    /// Wether the limb has found a grounded position that it can move to or not
    /// </summary>
    public bool HasAvailableGround { get; set; } = false;
    /// <summary>
    /// Wether the limb is currently doing a step or not
    /// </summary>
    public bool IsStepping { get; protected set; } = false;
    public Coroutine StepCoroutine { get; set; }
    /// <summary>
    /// True if the limb has no other solution than to have its knee bone in a wall
    /// </summary>
    public bool IsForcedIntoWall { get; set; } = false;
    /// <summary>
    /// Wether the limb is hidden or not
    /// </summary>
    public bool IsRetracted { get; set; } = false;
    /// <summary>
    /// Wether the limb is in the middle of hiding or not
    /// </summary>
    public bool IsRetracting { get; protected set; } = false;
    /// <summary>
    /// Wether the limb is in the middle of appearing or not
    /// </summary>
    public bool IsExtending { get; protected set; } = false;
    /// <summary>
    /// The progress of the limb being hidden or not. 0 is visible and 1 is fully hidden
    /// </summary>
    public float CurrentRetractation { get; protected set; } = 0f;
    public Coroutine RetractCoroutine { get; set; }

    public bool IsFlipping {  get; protected set; } = false;
    public Coroutine FlipCoroutine { get; set; }
    public float FlipCompletion { get; protected set; } = 1f;
    #endregion

    public Limb(Bone[] bones)
    {
        if (bones.Length != 3)
        {
            Debug.LogError($"A limb must contain 3 bones, this one contains {bones.Length}");
        }

        this.Bones = bones;
        HipBone = bones[0];
        FootBone = bones[2];
        KneeBone = bones[1];

        TargetPosition = FootBone.transform.position;
        LerpPosition = TargetPosition;

        Length = GetLimbLength();
        Direction = (FootBone.transform.position - HipBone.transform.position).normalized;
        FloatingDistance = 0.75f;
    }

    private float GetLimbLength()
    {
        float limbLength = 0f;

        for (int i = 0; i < Bones.Length; i++)
        {
            if (i == 0) continue;

            limbLength += Vector2.Distance(Bones[i].transform.position, Bones[i - 1].transform.position);
        }

        return limbLength;
    }

    /// <summary>
    /// Moves the limb's lerp position to its target position
    /// </summary>
    /// <param name="foundGroundedPosition">Wether the position to move to is grounded or not</param>
    public void MoveLerpPosition(bool foundGroundedPosition)
    {
        LerpPosition = TargetPosition;
        HasAvailableGround = foundGroundedPosition;
    }

    public IEnumerator Step(float stepDuration)
    {
        IsStepping = true;

        Vector2 initialPosition = IKTarget.transform.position;
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < stepDuration)
        {
            elapsedTime = Time.time - startTime;
            IKTarget.transform.position = Vector2.Lerp(initialPosition, LerpPosition, elapsedTime / stepDuration);

            yield return null;
        }

        IsStepping = false;
    }

    public IEnumerator RetractLimb(float duration, float targetRetractation)
    {
        if (CurrentRetractation == targetRetractation) yield break;

        IsRetracting = CurrentRetractation - targetRetractation < 0;
        IsExtending = !IsRetracting;

        float initialRetractation = CurrentRetractation;
        duration *= Mathf.Abs(CurrentRetractation - targetRetractation);
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            CurrentRetractation = Mathf.Lerp(initialRetractation, targetRetractation, elapsedTime / duration);
            yield return null;
        }

        CurrentRetractation = targetRetractation;

        IsRetracting = false;
        IsExtending = false;
    }

    public IEnumerator FlipKnee(float duration)
    {
        IsFlipping = true;

        duration *= FlipCompletion;

        FlipCompletion = 1 - FlipCompletion;

        float initialFlipCompletion = FlipCompletion;

        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            FlipCompletion = Mathf.Lerp(initialFlipCompletion, 1f, elapsedTime / duration);
            yield return null;
        }

        FlipCompletion = 1f;

        IsFlipping = false;
    }

    public Vector3 GetFlippedKneePosition()
    {
        Solver.flip = !Solver.flip;
        Solver.UpdateIK(1f);

        Vector3 position = KneeBone.transform.position;

        Solver.flip = !Solver.flip;
        Solver.UpdateIK(1f);

        return position;
    }

    public bool IsKneeInWall(LayerMask layerMask)
    {
        RaycastHit2D hit = Physics2D.Linecast(HipBone.transform.position, KneeBone.transform.position, layerMask);

        return hit;
    }
}
