using UnityEngine;

[RequireComponent (typeof(HingeJoint2D))]
public class SpringHingeJoint : MonoBehaviour
{
    public float currentAngle;
    public float jointSpeed;

    [Space]

    [SerializeField] float speed;

    private HingeJoint2D joint;
    private Rigidbody2D rb;
    private float targetRotation;

    private void Awake()
    {
        joint = GetComponent<HingeJoint2D>();
        rb = GetComponent<Rigidbody2D>();

        targetRotation = transform.localEulerAngles.z;
    }

    private void FixedUpdate()
    {
        currentAngle = joint.jointAngle;
        jointSpeed = joint.jointSpeed;

        JointMotor2D jointMotor = joint.motor;
        jointMotor.motorSpeed = (joint.referenceAngle - joint.jointAngle) * speed;

        joint.motor = jointMotor;
    }
}
