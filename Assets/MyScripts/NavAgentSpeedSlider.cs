using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Drives NavMeshAgent.speed from arrow keys / a UI Slider, and brakes into
// sharp path corners so the car stays on the road instead of cutting
// through turns at high speed.
public class NavAgentSpeedSlider : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Slider slider; // optional — keyboard control works without it
    [SerializeField] TextMeshProUGUI speedLabel; // optional — shown above the slider
    [SerializeField] float arrowKeyStep = 5f; // units per second
    [SerializeField] float minSpeed = 0f;
    [SerializeField] float maxSpeed = 30f;

    const float MetersPerSecondToMph = 2.23694f;

    [Header("Cornering")]
    [SerializeField] float cornerLookaheadDist = 10f; // start braking this far from a sharp corner
    [SerializeField] float cornerMinSpeedFactor = 0.35f; // slowest multiplier applied at a sharp corner
    [SerializeField] float sharpTurnAngle = 60f; // degrees — turns at/above this count as "sharp"

    [Header("Braking")]
    [SerializeField] float brakingDeceleration = 9f; // units/s^2 the car actually brakes at for any hazard below.
                                                       // Also used to size how far out braking starts, so a faster
                                                       // approach automatically starts braking sooner -- a fixed
                                                       // brake distance that works at low speed isn't enough room
                                                       // to stop in time at high speed otherwise.

    [Header("Pedestrian Avoidance")]
    [SerializeField] string pedestrianTag = "Pedestrian";
    [SerializeField] float pedestrianStopDist = 7f; // fully stopped by the time it's this close
    [SerializeField] float pedestrianAheadAngle = 70f; // degrees off forward still considered "ahead"

    [Header("Animal Avoidance")]
    [SerializeField] string animalTag = "Animal";
    [SerializeField] float animalStopDist = 7f; // fully stopped by the time it's this close
    [SerializeField] float animalAheadAngle = 70f; // degrees off forward still considered "ahead"

    [Header("Stop Signs")]
    [SerializeField] string stopSignTag = "StopSign";
    [SerializeField] float stopSignStopDist = 5f; // fully stopped by the time it's this close
    [SerializeField] float stopSignAheadAngle = 45f; // degrees off forward still considered "ahead"
    [SerializeField] float stopSignWaitSeconds = 2f; // how long to sit fully stopped before moving off again
    [SerializeField] float stopSignResetDist = 25f; // must clear this far past a sign before it can trigger again

    float desiredSpeed; // target cruise speed set by input, before corner/pedestrian/stop-sign braking
    float baseSpeed;
    float baseAngularSpeed;
    float baseAcceleration;
    Transform[] pedestrians; // lazily found by tag, like PedestrianCrossing finds the car
    Transform[] animals; // lazily found by tag, like AnimalCrossing finds the car
    Transform[] stopSigns; // lazily found by tag
    float[] stopSignWaitTimers; // seconds spent stopped at each sign, parallel to stopSigns
    bool[] stopSignCleared; // true once a sign has been waited out, until the car drives past it

    void Awake()
    {
        if (agent == null)
            agent = FindFirstObjectByType<NavMeshAgent>();

        desiredSpeed      = agent.speed;
        baseSpeed         = Mathf.Max(agent.speed, 0.01f);
        baseAngularSpeed  = agent.angularSpeed;
        baseAcceleration  = agent.acceleration;

        if (slider != null)
        {
            slider.minValue = minSpeed;
            slider.maxValue = maxSpeed;
            slider.value = desiredSpeed;
            slider.onValueChanged.AddListener(v => desiredSpeed = v);
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow))
            desiredSpeed = Mathf.Clamp(desiredSpeed + arrowKeyStep * Time.deltaTime, minSpeed, maxSpeed);
        else if (Input.GetKey(KeyCode.LeftArrow))
            desiredSpeed = Mathf.Clamp(desiredSpeed - arrowKeyStep * Time.deltaTime, minSpeed, maxSpeed);

        float hazardFactor = Mathf.Min(CornerSlowdownFactor(),
            Mathf.Min(PedestrianSlowdownFactor(), Mathf.Min(AnimalSlowdownFactor(), StopSignSlowdownFactor())));

        // Turning ability scales with speed (capped so it can't run away to
        // absurd values and cause jittery snap-turns at high cruise speed).
        float ratio = Mathf.Clamp(desiredSpeed / baseSpeed, 1f, 2.5f);
        agent.angularSpeed = baseAngularSpeed * ratio;

        // Brake harder than normal cruise acceleration whenever something
        // ahead is actively slowing the car down. Without this, the *target*
        // speed drops in time on paper but the NavMeshAgent physically can't
        // catch up to it fast enough at higher speeds, so the car still
        // barrels through the hazard even though the math says it shouldn't.
        agent.acceleration = hazardFactor < 1f ? brakingDeceleration : baseAcceleration * ratio;

        agent.speed = desiredSpeed * hazardFactor;

        if (slider != null)
            slider.SetValueWithoutNotify(desiredSpeed);

        if (speedLabel != null)
            speedLabel.text = $"{agent.velocity.magnitude * MetersPerSecondToMph:F0} mph";
    }

    // 1 = full speed, down to cornerMinSpeedFactor when a sharp corner is close ahead.
    float CornerSlowdownFactor()
    {
        if (!agent.hasPath || agent.path.corners.Length < 3)
            return 1f;

        Vector3[] corners = agent.path.corners;
        Vector3 toNextCorner = corners[1] - agent.transform.position;
        float distToCorner = toNextCorner.magnitude;

        if (distToCorner > cornerLookaheadDist)
            return 1f;

        Vector3 incoming = toNextCorner.normalized;
        Vector3 outgoing = (corners[2] - corners[1]).normalized;
        float turnAngle = Vector3.Angle(incoming, outgoing);

        if (turnAngle < sharpTurnAngle)
            return 1f;

        float sharpness = Mathf.InverseLerp(sharpTurnAngle, 150f, turnAngle); // 0..1
        float proximity = 1f - (distToCorner / cornerLookaheadDist);          // 0..1, 1 = at the corner

        return Mathf.Lerp(1f, cornerMinSpeedFactor, sharpness * proximity);
    }

    // Finds all active objects with the given tag, once. Shared by pedestrian
    // and stop-sign lookup — both just need "every hazard of this tag".
    static Transform[] FindHazardsByTag(string tag)
    {
        GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
        Transform[] transforms = new Transform[found.Length];
        for (int i = 0; i < found.Length; i++)
            transforms[i] = found[i].transform;
        return transforms;
    }

    // True if hazardPos is within brakeDist and inside the forward-facing
    // cone (ignores anything behind or well off to the side). distance is
    // always written, even when returning false, so callers can still use it
    // (e.g. to reset a timer once something falls out of range).
    bool IsAhead(Vector3 hazardPos, float brakeDist, float aheadAngle, out float distance)
    {
        Vector3 toHazard = hazardPos - agent.transform.position;
        distance = toHazard.magnitude;

        if (distance > brakeDist)
            return false;

        return Vector3.Angle(agent.transform.forward, toHazard) <= aheadAngle;
    }

    // How far out braking must start, given the current cruise speed, to
    // actually be stopped by stopDist at brakingDeceleration. This is why
    // braking now starts sooner automatically at higher speed instead of
    // using one fixed distance for every speed.
    float BrakingZoneDist(float stopDist)
    {
        return stopDist + (desiredSpeed * desiredSpeed) / (2f * brakingDeceleration);
    }

    // 1 = full speed, ramping down the *closer* the car gets -- not a
    // straight-line ramp over a fixed distance, but the actual max speed
    // physically stoppable in the remaining distance at brakingDeceleration.
    // That's what makes braking gentle far out and much more aggressive near
    // the hazard, and guarantees the car reaches ~0 by the time it's at
    // stopDist, no matter how fast it was going when it first noticed.
    float BrakingFactor(float distance, float stopDist)
    {
        float clearance = distance - stopDist;
        if (clearance <= 0f)
            return 0f;

        float maxSafeSpeed = Mathf.Sqrt(2f * brakingDeceleration * clearance);
        return Mathf.Clamp01(maxSafeSpeed / Mathf.Max(desiredSpeed, 0.01f));
    }

    // 1 = full speed, ramping down to 0 (full stop) as the nearest pedestrian
    // ahead gets close. Checks every pedestrian in the scene (not just the
    // first found) and brakes for whichever one demands it most.
    float PedestrianSlowdownFactor()
    {
        if (pedestrians == null)
            pedestrians = FindHazardsByTag(pedestrianTag);

        float minFactor = 1f;
        float brakeDist = BrakingZoneDist(pedestrianStopDist);

        foreach (Transform pedestrian in pedestrians)
        {
            if (pedestrian == null)
                continue;

            if (!IsAhead(pedestrian.position, brakeDist, pedestrianAheadAngle, out float distance))
                continue;

            float factor = BrakingFactor(distance, pedestrianStopDist);
            if (factor < minFactor)
                minFactor = factor;
        }

        return minFactor;
    }

    // Same as PedestrianSlowdownFactor, for the Animal tag (deer/dog/etc.
    // crossing via AnimalCrossing) — kept as its own pass rather than folded
    // into the pedestrian one so each hazard type can be tuned separately
    // (stop distance, cone angle) without affecting the other.
    float AnimalSlowdownFactor()
    {
        if (animals == null)
            animals = FindHazardsByTag(animalTag);

        float minFactor = 1f;
        float brakeDist = BrakingZoneDist(animalStopDist);

        foreach (Transform animal in animals)
        {
            if (animal == null)
                continue;

            if (!IsAhead(animal.position, brakeDist, animalAheadAngle, out float distance))
                continue;

            float factor = BrakingFactor(distance, animalStopDist);
            if (factor < minFactor)
                minFactor = factor;
        }

        return minFactor;
    }

    // 1 = full speed, ramping down to 0 (full stop) approaching a stop sign,
    // then held at 0 for stopSignWaitSeconds once actually stopped. After the
    // wait, the sign is marked "cleared" so the car pulls away and doesn't
    // re-trigger until it's driven stopSignResetDist past it (so the next
    // lap around the block stops again).
    float StopSignSlowdownFactor()
    {
        if (stopSigns == null)
        {
            stopSigns = FindHazardsByTag(stopSignTag);
            stopSignWaitTimers = new float[stopSigns.Length];
            stopSignCleared = new bool[stopSigns.Length];
        }

        float minFactor = 1f;
        float brakeDist = BrakingZoneDist(stopSignStopDist);

        for (int i = 0; i < stopSigns.Length; i++)
        {
            Transform sign = stopSigns[i];
            if (sign == null)
                continue;

            if (stopSignCleared[i])
            {
                float distanceToSign = Vector3.Distance(agent.transform.position, sign.position);
                if (distanceToSign > stopSignResetDist)
                {
                    stopSignCleared[i] = false;
                    stopSignWaitTimers[i] = 0f;
                }
                continue;
            }

            if (!IsAhead(sign.position, brakeDist, stopSignAheadAngle, out float distance))
            {
                if (distance > brakeDist)
                    stopSignWaitTimers[i] = 0f;
                continue;
            }

            // Detect "stopped at the sign" by actual velocity rather than an
            // exact distance crossing -- the ramp below only approaches
            // stopSignStopDist asymptotically as it nears 0, so the car can
            // sit essentially motionless just outside that exact threshold
            // and would otherwise never start (or finish) the wait timer.
            if (distance <= stopSignStopDist * 1.5f && agent.velocity.magnitude < 0.15f)
            {
                stopSignWaitTimers[i] += Time.deltaTime;
                if (stopSignWaitTimers[i] >= stopSignWaitSeconds)
                {
                    stopSignCleared[i] = true;
                    continue;
                }

                minFactor = 0f;
                continue;
            }

            float factor = BrakingFactor(distance, stopSignStopDist);
            if (factor < minFactor)
                minFactor = factor;
        }

        return minFactor;
    }
}
