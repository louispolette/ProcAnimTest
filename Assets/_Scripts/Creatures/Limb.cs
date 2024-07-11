using System;
using UnityEngine;
using UnityEngine.U2D.IK;

[Serializable]
public class Limb
{
    public Bone startBone;
    public Bone endBone;
    public Bone[] bones;
    public LineRenderer renderer;
    public Vector3 offsetFromRoot;
    public LimbSolver2D solver;
    public Transform IKTarget;

    public Limb(Bone[] bones)
    {
        this.bones = bones;
        startBone = bones[0];
        endBone = bones[bones.Length - 1];
        renderer = null;
        offsetFromRoot = endBone.transform.position - startBone.transform.position;
        solver = null;
        IKTarget = null;
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
}
