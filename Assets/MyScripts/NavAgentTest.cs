using UnityEngine;
using UnityEngine.AI;

public class NavAgentTest : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent agent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

         if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found");
            enabled = false; // Disable the script if no agent is found
            return;
        }

        // Check if a target is assigned
        if (target == null)
        {
            Debug.LogWarning("No target assigned");
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }
}
