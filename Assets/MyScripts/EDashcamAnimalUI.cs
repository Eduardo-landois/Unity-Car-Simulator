using UnityEngine;
using UnityEngine.UI;

// Bottom-of-screen animal-crossing badge, separate from EDashcamBottomUI's
// pedestrian icon so the two ground-truth hazards can be told apart at a
// glance. Lights up only when an animal is actually mid-crossing
// (AnimalCrossing.IsCrossing) AND currently visible to the dashcam camera —
// ground truth, not a guess at the AI caption's wording.
public class EDashcamAnimalUI : MonoBehaviour
{
    [SerializeField] Image animalIcon;

    [Header("Animal ground truth")]
    [SerializeField] Camera dashcamCamera;
    [SerializeField] string animalTag = "Animal";
    [SerializeField] LayerMask occlusionMask = ~0;

    struct AnimalRef
    {
        public Transform transform;
        public IAnimalHazard hazard;
    }

    AnimalRef[] animals;
    bool lastVisible;

    void Awake()
    {
        if (animalIcon != null)
            animalIcon.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateAnimalGroundTruth();
    }

    void UpdateAnimalGroundTruth()
    {
        if (animalIcon == null || dashcamCamera == null)
            return;

        if (animals == null)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(animalTag);
            animals = new AnimalRef[found.Length];
            for (int i = 0; i < found.Length; i++)
                animals[i] = new AnimalRef { transform = found[i].transform, hazard = found[i].GetComponent<IAnimalHazard>() };
        }

        bool anyVisible = false;
        foreach (AnimalRef animal in animals)
        {
            if (animal.hazard == null || !animal.hazard.IsCrossing)
                continue;

            if (IsVisibleToCamera(animal.transform))
            {
                anyVisible = true;
                break;
            }
        }

        animalIcon.gameObject.SetActive(anyVisible);

        if (anyVisible != lastVisible)
        {
            lastVisible = anyVisible;
            DashcamHistoryLogger.LogAnimalGroundTruth(anyVisible);
        }
    }

    bool IsVisibleToCamera(Transform animal)
    {
        Vector3 targetPoint = animal.position + Vector3.up * 1f;
        Vector3 viewportPoint = dashcamCamera.WorldToViewportPoint(targetPoint);

        if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        Vector3 origin = dashcamCamera.transform.position;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            bool hitIsAnimal = hit.transform.IsChildOf(animal) || hit.transform == animal;
            bool hitIsOwnCar = hit.transform.root == dashcamCamera.transform.root;
            if (!hitIsAnimal && !hitIsOwnCar)
                return false;
        }

        return true;
    }
}
