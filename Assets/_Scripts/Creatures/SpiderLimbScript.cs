using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D.IK;

public class SpiderLimbScript : MonoBehaviour
{
    [Header("Legs")]

    public Bone legRoot;
    [SerializeField] private int legAmount;
    public List<Limb> _limbs { get; private set; } = new List<Limb>();

    private List<Vector3> _idealLegPositions = new List<Vector3>();

    [Header("Debug")]

    [SerializeField] private bool _enableDebug = false;

    private void Awake()
    {
        CreateLimbs();
        GetIdealLegPositions();
    }

    #region rig building

    /// <summary>
    /// Sets up the limbs of the creature
    /// </summary>
    private void CreateLimbs()
    {
        Bone[] limbRoots = GetDirectChildBones(legRoot);

        foreach (Bone limbStart in limbRoots)
        {
            _limbs.Add(BuildLimb(limbStart));
        }
    }

    /// <summary>
    /// Builds a limb from every children of the selected bone
    /// </summary>
    /// <param name="limbStart">First bone of the limb</param>
    /// <returns></returns>
    private Limb BuildLimb(Bone limbStart)
    {
        List<Bone> limbBones = new();

        limbBones.Add(limbStart);

        GetAllChildBones(limbStart.transform, limbBones);

        return new Limb(limbBones.ToArray());
    }

    /// <summary>
    /// Returns the direct child bones of a bone
    /// </summary>
    /// <param name="bone"></param>
    /// <returns></returns>
    private Bone[] GetDirectChildBones(Bone bone)
    {
        List<Bone> childBones = new();

        bool hasChildBone = false;

        foreach (Transform child in bone.transform)
        {
            if (child.TryGetComponent(out Bone childBone))
            {
                hasChildBone = true;
                childBones.Add(childBone);
            }
        }

        bone.isEndBone = !hasChildBone;

        return childBones.ToArray();
    }

    /// <summary>
    /// Recursively gets all child bones of the given parent and puts them into the given list
    /// </summary>
    /// <param name="parent">Transform from which to get get bones</param>
    /// <param name="list">List that holds the child bones</param>
    private void GetAllChildBones(Transform parent, List<Bone> list)
    {
        if (parent.transform.childCount == 0)
        {
            if (parent.TryGetComponent(out Bone bone))
            {
                bone.isEndBone = true;
                return;
            }
        }

        foreach (Transform child in parent.transform)
        {
            if (child.TryGetComponent(out Bone childBone))
            {
                list.Add(childBone);
                GetAllChildBones(child, list);
            }
        }
    }

    #endregion

    private void GetIdealLegPositions()
    {
        foreach (Limb limb in _limbs)
        {
            _idealLegPositions.Add(limb.offsetFromRoot * 0.5f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_enableDebug) return;

        Gizmos.color = Color.red;

        foreach (Vector3 position in _idealLegPositions)
        {
            Gizmos.DrawSphere(legRoot.transform.position + position, 0.5f);
        }
    }
}
