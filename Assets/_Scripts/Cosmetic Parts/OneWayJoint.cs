using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OneWayJoint : MonoBehaviour
{
    [SerializeField] private Transform connectedTransform;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        rb.position = connectedTransform.position;
        //transform.position = connectedTransform.position;
    }

    public void SetConnectedTransform(Transform connectedTransform)
    {
        this.connectedTransform = connectedTransform;
    }
}
