using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NavAgentMultTargets : MonoBehaviour
{
    public Transform[] targets;
    private NavMeshAgent agent;

    int currentTarg = 0;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found");
            enabled = false; // Disable the script if no agent is found
            return;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        // Check if a target is assigned
        if (targets[currentTarg] == null)
        {
            Debug.LogWarning("No target assigned");
        }
        else
        {
            agent.SetDestination(targets[currentTarg].position);
        }

        if (currentTarg >= targets.Length)
        {
            Debug.Log("Final target reached");
            agent.isStopped = true;
        }
        else if (Vector3.Distance(this.transform.position, targets[currentTarg].transform.position) < 3.5)
        {
            Debug.Log("On to Next Target");

            /*if (currentTarg == 0)
            {
                StartCoroutine(StopForSeconds(2f));
            }
            else
            {*/
                currentTarg++;  
            //}

        }                 
    }

    IEnumerator StopForSeconds(float seconds)
    {
        agent.isStopped = true;

        yield return new WaitForSeconds(seconds);

        agent.isStopped = false;
        currentTarg++;
    }
}
