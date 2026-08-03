using ithappy.Animals_FREE;
using UnityEngine;

// "Deer in the headlights": darts from startPoint out to roadPoint when the
// car gets close, freezes there for a beat, then turns and bolts back to
// startPoint instead of continuing across — unlike AnimalCrossing, which
// walks a straight line all the way from one side to the other.
public class AnimalDashCrossing : MonoBehaviour, IAnimalHazard
{
    [SerializeField] private Vector3 startPoint;
    [SerializeField] private Vector3 roadPoint;
    [SerializeField] private float dashSpeed = 7f;
    [SerializeField] private float retreatSpeed = 5f;
    [SerializeField] private float freezeDuration = 1.2f;
    [SerializeField] private float triggerRadius = 25f;
    [SerializeField] private string carTag = "Player";
    [SerializeField] private bool crossOnlyOnce = true;
    [SerializeField] private float cooldownSeconds = 6f; // only used when crossOnlyOnce is false

    private static readonly int VertHash = Animator.StringToHash("Vert");
    private static readonly int StateHash = Animator.StringToHash("State");

    private enum Phase { Waiting, Dashing, Frozen, Retreating, Cooldown, Done }

    private Animator animator;
    private Transform car;
    private Phase phase = Phase.Waiting;
    private float timer;

    public bool IsCrossing => phase is Phase.Dashing or Phase.Frozen or Phase.Retreating;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        transform.position = startPoint;

        if (TryGetComponent(out MovePlayerInput input))
            input.enabled = false;
        if (TryGetComponent(out CreatureMover mover))
            mover.enabled = false;
    }

    private void Update()
    {
        if (car == null)
        {
            GameObject carObject = GameObject.FindGameObjectWithTag(carTag);
            if (carObject != null)
            {
                car = carObject.transform;
            }
        }

        switch (phase)
        {
            case Phase.Waiting:
                if (car != null && Vector3.Distance(car.position, startPoint) <= triggerRadius)
                {
                    BeginDash();
                }
                break;

            case Phase.Dashing:
                if (StepTowards(roadPoint, dashSpeed))
                {
                    BeginFreeze();
                }
                break;

            case Phase.Frozen:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    BeginRetreat();
                }
                break;

            case Phase.Retreating:
                if (StepTowards(startPoint, retreatSpeed))
                {
                    FinishRetreat();
                }
                break;

            case Phase.Cooldown:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    phase = Phase.Waiting;
                }
                break;

            case Phase.Done:
                break;
        }
    }

    private bool StepTowards(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < 0.05f;
    }

    private void BeginDash()
    {
        phase = Phase.Dashing;
        animator.SetFloat(StateHash, 1f);
        animator.SetFloat(VertHash, 1f);
    }

    private void BeginFreeze()
    {
        phase = Phase.Frozen;
        timer = freezeDuration;
        animator.SetFloat(VertHash, 0f);

        // Face whatever startled it, for the classic "frozen in the headlights" look.
        if (car != null)
        {
            Vector3 toCar = car.position - transform.position;
            toCar.y = 0f;
            if (toCar.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toCar);
            }
        }
    }

    private void BeginRetreat()
    {
        phase = Phase.Retreating;
        animator.SetFloat(StateHash, 1f);
        animator.SetFloat(VertHash, 1f);
    }

    private void FinishRetreat()
    {
        animator.SetFloat(VertHash, 0f);

        if (crossOnlyOnce)
        {
            phase = Phase.Done;
        }
        else
        {
            phase = Phase.Cooldown;
            timer = cooldownSeconds;
        }
    }
}
