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
            if (limb.HasNoValidPosition && !limb.IsRetracting && limb.CurrentRetractation < 1f)
            {
                StartRetractationCoroutine(limb, 1f);
            }
            else if (!limb.HasNoValidPosition && !limb.IsExtending && limb.CurrentRetractation > 0f)
            {
                StartRetractationCoroutine(limb, 0f);
            }

            bonePositions.Clear();

            for (int i = 0; i < limb.Bones.Length; i++)
            {
                Vector3 bonePosition = new();

                if (i == 1)
                {
                    bonePosition = Vector3.Lerp(limb.GetFlippedKneePosition(), limb.Bones[i].transform.position, 1 - (MathF.Cos(limb.FlipCompletion * MathF.PI) + 1) / 2);
                }
                else
                {
                    bonePosition = limb.Bones[i].transform.position;
                }

                bonePositions.Add(limb.HipBone.transform.position
                               + (bonePosition - limb.HipBone.transform.position) * (1 - limb.CurrentRetractation));
            }

            limb.Renderer.positionCount = bonePositions.Count;
            limb.Renderer.SetPositions(bonePositions.ToArray());
        }
    }

    private void StartRetractationCoroutine(Limb limb, float targetRetractation)
    {
        if (limb.RetractCoroutine != null)
        {
            StopCoroutine(limb.RetractCoroutine);
        }

        limb.RetractCoroutine = StartCoroutine(limb.RetractLimb(_limbScript.retractDuration, targetRetractation));
    }
}
