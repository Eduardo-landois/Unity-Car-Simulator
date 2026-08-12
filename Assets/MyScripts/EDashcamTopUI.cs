using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Owns the on-screen banner. Captions are always displayed in white.
/// If captionText is left empty it builds its own Canvas at runtime.
public class EDashcamTopUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI captionText;

    static readonly Color SafeColor = Color.white;

    string lastCaptionText;

    public void DisplayCaption(string text, long frameId)
    {
        SetCaption(text, frameId);
    }

    public void ShowError(string text, long frameId)
    {
        SetCaption(text, frameId);
    }

    void SetCaption(string text, long frameId)
    {
        if (captionText == null)
            return;

        // Skip redundant updates — same caption as last time, don't touch the UI.
        if (text == lastCaptionText)
            return;

        lastCaptionText = text;

        captionText.text  = text;
        captionText.color = SafeColor;

        DashcamHistoryLogger.LogUIDisplay(text, frameId);
    }
}
