using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Owns the on-screen banner and the decision of what counts as a hazard.
/// If captionText is left empty it builds its own Canvas at runtime.
///
/// The AI only returns freeform caption text — this is the one place that
/// turns it into a red/white hazard verdict, by keyword-matching the
/// caption (see IsHazard).
public class EDashcamTopUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI captionText;

    static readonly string[] HazardKeywords =
    {
        "pedestrian", "person","stop sign", "red light", "obstacle",
        "brake", "caution", "warning", "danger", "cyclist", "emergency", "crossing",
        "animal", "deer", "dog", "wildlife",
    };

    static readonly Color SafeColor   = Color.white;
    static readonly Color HazardColor = Color.red;

    string lastCaptionText;

    public void DisplayCaption(string text, long frameId)
    {
        SetCaption(text, IsHazard(text), frameId);
    }

    public void ShowError(string text, long frameId)
    {
        SetCaption(text, isHazard: false, frameId);
    }

    static bool IsHazard(string caption)
    {
        if (string.IsNullOrEmpty(caption))
            return false;

        string lower = caption.ToLowerInvariant();
        foreach (string kw in HazardKeywords)
            if (lower.Contains(kw))
                return true;

        return false;
    }

    void SetCaption(string text, bool isHazard, long frameId)
    {
        if (captionText == null)
            return;

        // Skip redundant updates — same caption as last time, don't touch the UI.
        if (text == lastCaptionText)
            return;

        lastCaptionText = text;

        captionText.text  = text;
        captionText.color = isHazard ? HazardColor : SafeColor;

        DashcamHistoryLogger.LogUIDisplay(text, frameId);
    }
}
