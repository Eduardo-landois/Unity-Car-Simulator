using UnityEngine;
using MVC.Core;

public class SteeringWheelRotationManager : MonoBehaviour
{
    public Transform targetSteeringWheel;
    public Vector3 rotLocalCenter;
    public Vector3 rotLocalAxis;

    public Vehicle vehicle;
    public float maxRotationAngle = 450f;
    public bool invert;

    Quaternion restLocalRotation;

    void Start()
    {
        if (targetSteeringWheel == null)
            targetSteeringWheel = transform;

        if (vehicle == null)
            vehicle = GetComponentInParent<Vehicle>();

        restLocalRotation = targetSteeringWheel.localRotation;
    }

    void LateUpdate()
    {
        if (targetSteeringWheel == null || vehicle == null || vehicle.SteerWheels == null || vehicle.SteerWheels.Length == 0)
            return;

        VehicleWheel wheel = vehicle.SteerWheels[0].Instance;
        if (wheel == null)
            return;

        // CurrentSteerAngle reflects the real physics steer angle no matter what
        // is driving the car (player input, AI/NavMeshAgent, gamepad, etc.).
        float maxSteerAngle = Mathf.Max(vehicle.Steering.MaximumSteerAngle, 0.01f);
        float steerInput = Mathf.Clamp(wheel.CurrentSteerAngle / maxSteerAngle, -1f, 1f);
        if (invert)
            steerInput = -steerInput;

        float angle = steerInput * maxRotationAngle;

        targetSteeringWheel.localRotation = restLocalRotation;

        Vector3 rotGlobalCenter = targetSteeringWheel.TransformPoint(rotLocalCenter);
        Vector3 rotGlobalAxis = targetSteeringWheel.TransformDirection(rotLocalAxis);
        targetSteeringWheel.RotateAround(rotGlobalCenter, rotGlobalAxis, angle);
    }

    void OnDrawGizmosSelected()
    {
        if (targetSteeringWheel == null)
            return;

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
