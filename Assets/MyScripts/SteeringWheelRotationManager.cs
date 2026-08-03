using UnityEngine;

public class SteeringWheelRotationManager : MonoBehaviour
{
    public Transform targetSteeringWheel;
    public Vector3 rotLocalCenter;
    public Vector3 rotLocalAxis;


    private float rotAngleDeg;

    private float rotSpeed = 50f;

    private float currentAngle = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // same position and rotation as steering wheel
        if (this.targetSteeringWheel != null)
        {
            Vector3 posSteeringWheel = this.targetSteeringWheel.position;

            if (Input.GetKey(KeyCode.A))
            {
                rotAngleDeg = -rotSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                rotAngleDeg = rotSpeed * Time.deltaTime;
            }
            else // returning wheel to center
            {
                if (Mathf.Abs(currentAngle) > 0.1f)
                {
                    rotAngleDeg = rotSpeed * Time.deltaTime * -Mathf.Sign(currentAngle);

                    if (Mathf.Abs(rotAngleDeg) > Mathf.Abs(currentAngle))
                    {
                        rotAngleDeg = -currentAngle;
                    }
                }
                else
                {
                    currentAngle = 0f;
                    return;
                }
            }

            Vector3 rotGlobalCenter = this.targetSteeringWheel.transform.TransformPoint(this.rotLocalCenter);
            Vector3 rotGlobalAxis = this.targetSteeringWheel.transform.TransformDirection(this.rotLocalAxis);
            this.targetSteeringWheel.transform.RotateAround(rotGlobalCenter, rotGlobalAxis, rotAngleDeg);

            currentAngle += rotAngleDeg;

        }

    }

    void OnDrawGizmosSelected()
    {
        // Manually configuring local rotation axis.
        Gizmos.color = new Color(1, 1, 0, 0.75F);
        Vector3 rotGlobalAxis = this.targetSteeringWheel.transform.TransformDirection(this.rotLocalAxis);
        Gizmos.DrawLine(this.targetSteeringWheel.position + new Vector3(1.2f, 0, 0),
                        this.targetSteeringWheel.position + rotGlobalAxis + new Vector3 (1.2f, 0, 0));
        // Manually configuring local rotation center.
        Gizmos.color = new Color(1, 0, 0, 0.5F);
        Vector3 rotGlobalCenter = this.targetSteeringWheel.transform.TransformPoint(this.rotLocalCenter);
        Gizmos.DrawSphere(rotGlobalCenter, .001f);
        Gizmos.DrawLine(this.targetSteeringWheel.position, rotGlobalCenter);
    }

}
