using System.Collections;
using UnityEngine;

// Orchestrates the dashcam pipeline via three independent coroutine loops —
// capture, send/receive, UI — each on its own tunable interval. Passes the
// latest result from each stage to the next, like a messenger between the
// other EDashcam* scripts.

public class EDashcamController : MonoBehaviour 
{
    [SerializeField] EDashcamCapture capture;   // Snapshot
    [SerializeField] EDashcamAIClient aiClient;  // AI
    [SerializeField] EDashcamTopUI Tui;  //Top UI

    [Header("Intervals (seconds)")]
    public float captureInterval = 0.1f;
    public float AIInterval    = 0.1f;
    public float uiInterval      = 0.1f;

    [Header("History Logging")]
    [SerializeField] bool enableHistoryLogging = true;

    string latestFrameB64;
    long   latestFrameId;
    long   frameCounter;
    string latestCaption;
    long   latestCaptionFrameId;
    bool   latestIsError;
    bool   sending;

    public string GetCaption() => latestCaption;

    void Start()
    {

        DashcamHistoryLogger.Enabled = enableHistoryLogging;

        StartCoroutine(CaptureLoop());
        StartCoroutine(SendLoop());
        StartCoroutine(UILoop());
    }

    // Synced every frame so the Inspector checkbox can be toggled live
    // during Play mode, not just before pressing Play.
    void Update()
    {
        DashcamHistoryLogger.Enabled = enableHistoryLogging;
    }

    IEnumerator CaptureLoop()
    {
        while (true)
        {
            latestFrameB64 = capture.CaptureFrameBase64();
            latestFrameId  = ++frameCounter;
            DashcamHistoryLogger.LogSnapshot(latestFrameB64, latestFrameId);
            yield return new WaitForSeconds(captureInterval);
        }
    }

    IEnumerator SendLoop()
    {
        while (true)
        {
            // Guard against overlapping requests — if the previous send
            // hasn't finished, skip this tick rather than stacking another.
            if (!sending && latestFrameB64 != null)
                StartCoroutine(SendCurrentFrame());

            yield return new WaitForSeconds(AIInterval);
        }
    }

    IEnumerator SendCurrentFrame()
    {
        sending = true;
        long frameIdForThisSend = latestFrameId;
        yield return aiClient.SendRequest(latestFrameB64, frameIdForThisSend, (caption, isError) =>
        {
            latestCaption        = caption;
            latestCaptionFrameId = frameIdForThisSend;
            latestIsError        = isError;
            if (!isError)
                DashcamHistoryLogger.LogCaption(caption, frameIdForThisSend);
        });
        sending = false;
    }

    IEnumerator UILoop()
    {
        while (true)
        {
            if (latestCaption != null)
            {
                if (latestIsError)
                    Tui.ShowError(latestCaption, latestCaptionFrameId);
                else
                    Tui.DisplayCaption(latestCaption, latestCaptionFrameId);
            }
            yield return new WaitForSeconds(uiInterval);
        }
    }
}
