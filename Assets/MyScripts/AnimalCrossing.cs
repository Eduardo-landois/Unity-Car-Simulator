using ithappy.Animals_FREE;
using UnityEngine;

// Scripted animal crossing, mirrors PedestrianCrossing.cs. The Animals_FREE
// animator controllers use float params (Vert = idle/moving blend, State =
// walk/run blend) instead of the bool "Walking" the pedestrian rig uses.
public class AnimalCrossing : MonoBehaviour, IAnimalHazard
{
    [SerializeField] private Vector3 startPoint;
    [SerializeField] private Vector3 endPoint;
    [SerializeField] private float walkSpeed = 1.4f;
    [SerializeField] private float triggerRadius = 25f;
    [SerializeField] private string carTag = "Player";
    [SerializeField] private bool crossOnlyOnce = true;

    private static readonly int VertHash = Animator.StringToHash("Vert");
    private static readonly int StateHash = Animator.StringToHash("State");

    private Animator animator;
    private Transform car;
    private bool isCrossing;
    private bool hasCrossed;

    public bool IsCrossing => isCrossing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        transform.position = startPoint;

        // Animal prefabs ship with player-input locomotion enabled by
        // default; disable it so this script has sole control of movement.
        if (TryGetComponent(out MovePlayerInput input))
            input.enabled = false;
        if (TryGetComponent(out CreatureMover mover))
            mover.enabled = false;
    }

    private void Update()
    {
        if (hasCrossed)
        {
            return;
        }

        if (car == null)
        {
            GameObject carObject = GameObject.FindGameObjectWithTag(carTag);
            if (carObject != null)
            {
                car = carObject.transform;
            }
        }

        if (isCrossing)
        {
            StepAcross();
            return;
        }

        if (car == null)
        {
            return;
        }

        if (Vector3.Distance(car.position, startPoint) <= triggerRadius)
        {
            isCrossing = true;
            animator.SetFloat(StateHash, 0f);
            animator.SetFloat(VertHash, 1f);

            Vector3 direction = endPoint - startPoint;
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void StepAcross()
    {
        transform.position = Vector3.MoveTowards(transform.position, endPoint, walkSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, endPoint) < 0.05f)
        {
            isCrossing = false;
            animator.SetFloat(VertHash, 0f);

            if (crossOnlyOnce)
            {
                hasCrossed = true;
            }
            else
            {
                transform.position = startPoint;
            }
        }
    }
}
