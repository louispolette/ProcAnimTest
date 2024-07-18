using UnityEngine;

public class CosmeticPart : MonoBehaviour
{
    [SerializeField] private OneWayJoint rootObject;

    public void SetConnectedTransform(Transform connectedTransform)
    {
        rootObject.SetConnectedTransform(connectedTransform);
    }
}
