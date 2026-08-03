using UnityEngine;
using UnityEngine.UI;

// Bottom-of-screen pedestrian-crossing badge. Lights up only when a
// pedestrian is actually mid-crossing (PedestrianCrossing.IsCrossing) AND
// currently visible to the dashcam camera (in frustum, not blocked by scene
// geometry) — ground truth, not a guess at the AI caption's wording.
public class EDashcamBottomUI : MonoBehaviour
{
    [SerializeField] Image pedestrianIcon;

    [Header("Pedestrian ground truth")]
    [SerializeField] Camera dashcamCamera;
    [SerializeField] string pedestrianTag = "Pedestrian";
    [SerializeField] LayerMask occlusionMask = ~0;

    PedestrianCrossing[] pedestrians;
    bool lastVisible;

    void Awake()
    {
        if (pedestrianIcon != null)
            pedestrianIcon.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdatePedestrianGroundTruth();
    }

    // Ground truth: lights up only while a pedestrian is actually mid-crossing
    // AND currently visible to the dashcam camera (in frustum, not blocked by
    // scene geometry) — doesn't depend on the VLM caption at all.
    void UpdatePedestrianGroundTruth()
    {
        if (pedestrianIcon == null || dashcamCamera == null)
            return;

        if (pedestrians == null)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(pedestrianTag);
            pedestrians = new PedestrianCrossing[found.Length];
            for (int i = 0; i < found.Length; i++)
                pedestrians[i] = found[i].GetComponent<PedestrianCrossing>();
        }

        bool anyVisible = false;
        foreach (PedestrianCrossing pedestrian in pedestrians)
        {
            if (pedestrian == null || !pedestrian.IsCrossing)
                continue;

            if (IsVisibleToCamera(pedestrian.transform))
            {
                anyVisible = true;
                break;
            }
        }

        pedestrianIcon.gameObject.SetActive(anyVisible);

        if (anyVisible != lastVisible)
        {
            lastVisible = anyVisible;
            DashcamHistoryLogger.LogPedestrianGroundTruth(anyVisible);
        }
    }

    bool IsVisibleToCamera(Transform pedestrian)
    {
        Vector3 targetPoint = pedestrian.position + Vector3.up * 1f;
        Vector3 viewportPoint = dashcamCamera.WorldToViewportPoint(targetPoint);

        if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        Vector3 origin = dashcamCamera.transform.position;
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            bool hitIsPedestrian = hit.transform.IsChildOf(pedestrian) || hit.transform == pedestrian;
            bool hitIsOwnCar = hit.transform.root == dashcamCamera.transform.root;
            if (!hitIsPedestrian && !hitIsOwnCar)
                return false;
        }

        return true;
    }
}
