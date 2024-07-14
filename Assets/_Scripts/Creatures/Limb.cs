using System;
using System.Collections;
using UnityEngine;
using UnityEngine.U2D.IK;

[Serializable]
public class Limb
{
    public Bone[] bones;
    public Bone startBone;
    public Bone endBone;
    public Bone elbowBone;
    public LineRenderer renderer;
    public Vector3 offsetFromRoot;
    public float floatingDistance;
    public LimbSolver2D solver;
    public Transform IKTarget;
    public Vector2 direction;
    public float length;
    public Vector2 targetPosition;
    public Vector2 lerpPosition;
    public bool hasAvailableGround;
    public Coroutine stepCoroutine;
    public bool isStepping;
    public bool isForcedIntoWall;
    public bool isRetracted;
    public Coroutine retractCoroutine;
    public bool isRetracting;
    public bool isExtending;
    public float currentRetractation;

    public Limb(Bone[] bones)
    {
        this.bones = bones;
        startBone = bones[0];
        endBone = bones[bones.Length - 1];
        elbowBone = bones[1];
        renderer = null;
        offsetFromRoot = endBone.transform.position - startBone.transform.position;
        floatingDistance = 0.75f;
        solver = null;
        IKTarget = null;
        direction = Vector2.up;
        length = GetLimbLength();
        targetPosition = endBone.transform.position;
        lerpPosition = targetPosition;
        hasAvailableGround = false;
        stepCoroutine = null;
        isStepping = false;
        isForcedIntoWall = false;
        isRetracted = false;
        retractCoroutine = null;
        isRetracting = false;
        currentRetractation = 0;
    }

    public float GetLimbLength()
    {
        float limbLength = 0f;

        for (int i = 0; i < bones.Length; i++)
        {
            if (i == 0) continue;

            limbLength += Vector2.Distance(bones[i].transform.position, bones[i - 1].transform.position);
        }

        return limbLength;
    }

    /// <summary>
    /// Moves the limb's lerp position to its target position
    /// </summary>
    /// <param name="foundGroundedPosition">Wether the position to move to is grounded or not</param>
    public void MoveLerpPosition(bool foundGroundedPosition)
    {
        lerpPosition = targetPosition;
        hasAvailableGround = foundGroundedPosition;
    }

    public IEnumerator Step(float stepDuration)
    {
        isStepping = true;

        Vector2 initialPosition = IKTarget.transform.position;
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < stepDuration)
        {
            elapsedTime = Time.time - startTime;
            IKTarget.transform.position = Vector2.Lerp(initialPosition, lerpPosition, elapsedTime / stepDuration);

            yield return null;
        }

        isStepping = false;
    }

    public IEnumerator RetractLimb(float duration, float targetRetractation)
    {
        if (currentRetractation == targetRetractation) yield break;

        isRetracting = currentRetractation - targetRetractation < 0;
        isExtending = !isRetracting;

        duration *= Mathf.Abs(currentRetractation - targetRetractation);
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            currentRetractation = Mathf.Lerp(currentRetractation, targetRetractation, elapsedTime / duration);
            yield return null;
        }

        currentRetractation = targetRetractation;

        isRetracting = false;
        isExtending = false;
    }

    public bool IsElbowIsInWall(LayerMask layerMask)
    {
        RaycastHit2D hit = Physics2D.Linecast(startBone.transform.position, elbowBone.transform.position, layerMask);

        return hit;
    }
}
