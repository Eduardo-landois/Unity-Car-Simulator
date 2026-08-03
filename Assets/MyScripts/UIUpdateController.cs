/*using System.Collections; // To access C# IEnumerator and yield return.
using UnityEngine; // To access Unity classes.
using UnityEngine.UI; // To access Unity UI classes.
using TMPro; // To access TextMeshPro classes.

// Script to be attached to the main canvas.
public class UIUpdateController : MonoBehaviour {
    

    public TextMeshProUGUI uiCaption; // To be configured by developer (in Editor).
    public DashcamHazardHUD aiinputOutputController; // To be configured by developer (in Editor). 

    // ...
    void Start() {
        // DEBUG
        this.uiCaption.color = Color.green; // Example of UI customization.
        //this.uiCaption.text = "Testing started...";
        string caption = aiinputOutputController.GetCaption(); // Asking AI controller to provide current caption.
        // Checking if AI caption is valid.
        if(caption != null) {
            this.uiCaption.text = caption; // Updating UI caption with AI caption.
        } else {
            this.uiCaption.text = "No caption available.";
        }
    }

    // ...
    void Update() {}

    // Coroutine to schedule updates to the UI.
    private IEnumerator UpdateUI() {
        /*
        Color c = renderer.material.color;
        for (float alpha = 1f; alpha >= 0; alpha -= 0.1f)
        {
            c.a = alpha;
            renderer.material.color = c;
            // Wait for 0.1 seconds before the next iteration
            yield return new WaitForSeconds(.1f);
        }
        
        yield return null; // DEBUG
    }

}
*/