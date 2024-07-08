using UnityEngine;

public class LegRenderer : MonoBehaviour
{
    private LineRenderer[] _lineRenderers;

    [SerializeField] private Transform[] _legBones;

    private void Start()
    {
        _lineRenderers = GetComponentsInChildren<LineRenderer>();
    }

    private void Update()
    {
        foreach (LineRenderer r in _lineRenderers)
        {
            r.positionCount = _legBones.Length;

        }
    }
}
