using UnityEngine;

public class PedestrianCrossing : MonoBehaviour
{
    [SerializeField] private Vector3 startPoint = new Vector3(-408.85f, 69.8f, 46f);
    [SerializeField] private Vector3 endPoint = new Vector3(-392.85f, 69.8f, 46f);
    [SerializeField] private float walkSpeed = 1.4f;
    [SerializeField] private float triggerRadius = 25f;
    [SerializeField] private string carTag = "Player";
    [SerializeField] private bool crossOnlyOnce = true;

    private static readonly int WalkingHash = Animator.StringToHash("Walking");

    private Animator animator;
    private Transform car;
    private bool isCrossing;
    private bool hasCrossed;

    public bool IsCrossing => isCrossing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        transform.position = startPoint;
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
            animator.SetBool(WalkingHash, true);
        }
    }

    private void StepAcross()
    {
        transform.position = Vector3.MoveTowards(transform.position, endPoint, walkSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, endPoint) < 0.05f)
        {
            isCrossing = false;
            animator.SetBool(WalkingHash, false);

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
