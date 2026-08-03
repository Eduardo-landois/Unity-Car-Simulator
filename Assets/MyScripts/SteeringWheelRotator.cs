//using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class SteeringWheelRotator : MonoBehaviour
{
    Collider wheel_Collider;
    Vector3 wheel_Center;
    public float rotationSpeed = 50f;

    Quaternion axisAngle = Quaternion.Euler(0f, 0f, 53.4970741f);
    
    void Start()
    {

    } 
    void Update()
    {
        wheel_Collider = GetComponent<Collider>();
        wheel_Center = wheel_Collider.bounds.center;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.RotateAround(wheel_Center, axisAngle * Vector3.down, rotationSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.RotateAround(wheel_Center, axisAngle * Vector3.down, -rotationSpeed * Time.deltaTime);
        }

    }
}

