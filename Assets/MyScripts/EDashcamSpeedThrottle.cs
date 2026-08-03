using UnityEngine;
using UnityEngine.AI;

// Scales EDashcamController's capture/AI request frequency
// based on the car's current NavMeshAgent speed — fast intervals while
// moving, slower (but not zero) while stopped, since a stationary car can
// still face hazards like a pedestrian crossing.
//
// Toggle in the Inspector to turn it
// on/off. While disabled, EDashcamController just uses whatever
// captureInterval/AIInterval you've set manually.
public class EDashcamSpeedThrottle : MonoBehaviour
{
    [SerializeField] EDashcamController dashcamController;
    [SerializeField] NavMeshAgent agent;

    [Header("Interval range (seconds)")]
    [SerializeField] float minInterval = 0.1f; // used at/above fastSpeedThreshold
    [SerializeField] float maxInterval = 1f;   // used at 0 speed
    [SerializeField] float fastSpeedThreshold = 15f; // units per second or 33.5 mph
                    // so if the car reaches the fastSpeedThreshold, the capture/AI request interval will 
                    // be at its minimum (max frequency)
    float defaultCaptureInterval;
    float defaultAIInterval;

    void Awake()
    {
        if (agent == null)
            agent = FindFirstObjectByType<NavMeshAgent>();
    }

    void OnEnable()
    {
        defaultCaptureInterval = dashcamController.captureInterval;
        defaultAIInterval      = dashcamController.AIInterval;
    }

    void OnDisable()
    {
        if (dashcamController == null)
            return;

        dashcamController.captureInterval = defaultCaptureInterval;
        dashcamController.AIInterval      = defaultAIInterval;
    }

    void Update()
    {
        // 0 = stopped, 1 = at/above fastSpeedThreshold.
        float speedFraction = Mathf.Clamp01(agent.speed / fastSpeedThreshold);   // make sure result is between 0 and 1, 
        // if speed is greater than fastSpeedThreshold then it will return 1, if speed is less than 0 then it will return 0, 
        // if speed is between 0 and fastSpeedThreshold then it will return a fraction between 0 and 1

        // Faster car -> shorter interval (more frequent captures/requests).
        float interval = Mathf.Lerp(maxInterval, minInterval, speedFraction);// =  a + (b-a) * c   if c = 0 then returns a, 
        //                              a           b             c          if c = 1 then returns b, if c = 0.5 then returns midpoint between a and b

        dashcamController.captureInterval = interval;
        dashcamController.AIInterval      = interval;
    }
}
