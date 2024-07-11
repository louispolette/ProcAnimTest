using System;
using System.Collections.Generic;
using UnityEngine;

public class LegRenderer : MonoBehaviour
{
    private SpiderLimbScript _limbScript;

    private List<Limb> _limbs;

    private List<Vector3> bonePositions = new List<Vector3>();

    [Serializable]
    public class RendererProperties
    {
        public Gradient color;
        public int cornerVertices;
        public int endCapVertices;
    }

    private void Awake()
    {
        _limbScript = GetComponent<SpiderLimbScript>();
    }

    private void OnEnable()
    {
        _limbScript.onLimbsSetupDone += Init;
    }

    private void OnDisable()
    {
        _limbScript.onLimbsSetupDone -= Init;
    }

    private void Init()
    {
        _limbs = _limbScript._limbs;
    }

    private void Update()
    {
        foreach (Limb limb in _limbs)
        {
            bonePositions.Clear();

            foreach (Bone bone in limb.bones)
            {
                bonePositions.Add(bone.transform.position);
            }

            limb.renderer.positionCount = bonePositions.Count;
            limb.renderer.SetPositions(bonePositions.ToArray());
        }
    }
}
