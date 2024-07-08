using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Limb
{
    public Bone[] bones;
    public Bone endBone;

    public Limb(Bone[] bones)
    {
        this.bones = bones;
        endBone = null;
    }
}
