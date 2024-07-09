using System;
using UnityEngine;

[Serializable]
public class Limb
{
    public Bone startBone;
    public Bone endBone;
    public Bone[] bones;
    public LineRenderer renderer;

    public Limb(Bone[] bones)
    {
        this.bones = bones;
        startBone = bones[0];
        endBone = bones[bones.Length - 1];
        renderer = null;
    }
}
