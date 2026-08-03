using UnityEngine;

public class ForceRigidBody : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;   
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
