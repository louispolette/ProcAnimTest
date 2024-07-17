using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D.IK;

[Serializable]
public class Limb
{
    #region members
    public int ID { get; } = -1;
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
    public bool IsFloating { get; set; }

    /// <summary>
    /// The position that the limb will move to when it is asked to take a step
    /// </summary>
    public Vector2 TargetPosition { get; set; }
    /// <summary>
    /// The position that the limb's foot is continually moving towards
    /// </summary>
    public Vector2 LerpPosition { get; set; }

    /// <summary>
    /// Wether the limb's lerp position is grounded or not
    /// </summary>
    public bool LerpPositionIsGrounded { get; set; } = false;
    /// <summary>
    /// This value is false when the limb could not find a suitable position and is either retracted or using another limb's target position
    /// </summary>
    public bool IsInIdealPosition { get; set; } = true;
    /// <summary>
    /// Wether the limb is currently doing a step or not
    /// </summary>
    public bool IsStepping { get; protected set; } = false;
    public Coroutine StepCoroutine { get; set; }
    public bool HasNoValidPosition { get; set; } = false;
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

    public Limb(Bone[] bones, int ID = -1)
    {
        if (bones.Length != 3)
        {
            Debug.LogError($"A limb must contain 3 bones, this one contains {bones.Length}");
        }

        this.ID = ID;

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
    /// <param name="targetPositionIsGrounded">Wether the position to move to is grounded or not</param>
    public void MoveLerpPosition(bool targetPositionIsGrounded)
    {
        LerpPosition = TargetPosition;
        LerpPositionIsGrounded = targetPositionIsGrounded;
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

        IKTarget.transform.position = LerpPosition;

        IsFloating = !LerpPositionIsGrounded;

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

    /// <summary>
    /// Does a linecast between the hip bone and the knee bone to determine if the later is in/behind a collider
    /// </summary>
    /// <param name="layerMask">The layers to take into consideration</param>
    /// <returns>Wether the knee bone is in/behind a collider or not</returns>
    public bool IsKneeInWall(LayerMask layerMask)
    {
        RaycastHit2D hit = Physics2D.Linecast(HipBone.transform.position, KneeBone.transform.position, layerMask);

        return hit;
    }

    /// <summary>
    /// Checks wether the hip bone is in a collider or not
    /// </summary>
    /// <param name="layerMask">The layers to take into consideration</param>
    /// <returns>Wether the hip bone is in a collider or not</returns>
    public bool IsHipInWall(LayerMask layerMask)
    {
        Collider2D hit = Physics2D.OverlapCircle(HipBone.transform.position, 0f, layerMask);

        return hit;
    }
}
