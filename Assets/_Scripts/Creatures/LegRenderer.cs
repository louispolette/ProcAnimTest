using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.U2D.IK;

public class LegRenderer : MonoBehaviour
{
    private SpiderLimbScript _limbScript;

    private LineRenderer[] _lineRenderers;

    private Bone _rootBone;

    [SerializeField] public List<Limb> _limbs;

    private List<Vector3> bonePositions = new List<Vector3>();

    private void Awake()
    {
        _limbScript = GetComponent<SpiderLimbScript>();
    }

    private void Start()
    {
        _rootBone = _limbScript.legRoot;
        _limbs = _limbScript._limbs;
        _lineRenderers = GetLineRenderers();
    }

    private LineRenderer[] GetLineRenderers()
    {
        List<LineRenderer> renderers = new List<LineRenderer>();

        foreach (Limb limb in _limbs)
        {
            limb.renderer = limb.startBone.GetComponent<LineRenderer>();
            renderers.Add(limb.renderer);
        }

        return renderers.ToArray();
    }

    private void Update()
    {
        foreach (Limb limb in _limbs)
        {
            bonePositions.Clear();

            bonePositions.Add(_rootBone.transform.position);

            foreach (Bone bone in limb.bones)
            {
                bonePositions.Add(bone.transform.position);
            }

            limb.renderer.positionCount = bonePositions.Count;
            limb.renderer.SetPositions(bonePositions.ToArray());
        }
    }
}
