using TMPro;
using UnityEngine;

public class CaptionDisplay : MonoBehaviour
{
    public TextMeshProUGUI captionText;

    public void SetCaption(string message)
    {
        captionText.text = message;
    }
}