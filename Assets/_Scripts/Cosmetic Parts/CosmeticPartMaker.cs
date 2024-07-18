using UnityEngine;

public class CosmeticPartMaker : MonoBehaviour
{
    [SerializeField] private CosmeticPart partPrefab;

    private void Awake()
    {
        CosmeticPart part = Instantiate(partPrefab);
        part.SetConnectedTransform(transform);
    }
}
