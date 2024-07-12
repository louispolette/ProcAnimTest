using System;
using UnityEngine;
using UnityEngine.U2D.IK;
using UnityEngine.UIElements;

[Serializable]
public class Limb
{
    public Bone startBone;
    public Bone endBone;
    public Bone[] bones;
    public LineRenderer renderer;
    public Vector3 offsetFromRoot;
    public float defaultDistance;
    public LimbSolver2D solver;
    public Transform IKTarget;
    public Vector2 direction;
    public float length;
    public Vector2 targetPosition;
    public Vector2 lerpPosition;
    public bool isOnGround;

    public Limb(Bone[] bones)
    {
        this.bones = bones;
        startBone = bones[0];
        endBone = bones[bones.Length - 1];
        renderer = null;
        offsetFromRoot = endBone.transform.position - startBone.transform.position;
        defaultDistance = 0.75f;
        solver = null;
        IKTarget = null;
        direction = Vector2.up;
        length = GetLimbLength();
        targetPosition = endBone.transform.position;
        lerpPosition = targetPosition;
        isOnGround = false;
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
    /// <param name="isPositionGrounded">Wether the position to move to is grounded or not</param>
    public void MoveLerpPosition(bool isPositionGrounded)
    {
        lerpPosition = targetPosition;
        isOnGround = isPositionGrounded;

        Debug.Log("Leg Moving");
    }
}
