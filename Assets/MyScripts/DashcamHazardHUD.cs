using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// Attach to any GameObject. Drag the hood Camera into hoodCam.
/// If captionText is left empty the script builds its own Canvas at runtime.
public class DashcamHazardHUD : MonoBehaviour
{
    [SerializeField] Camera hoodCam;
    [SerializeField] TextMeshProUGUI captionText;

    // ── constants ──────────────────────────────────────────────────────────
    const string ServerUrl   = "http://localhost:8080/v1/chat/completions";
    const int    TexWidth    = 280; //250
    const int    TexHeight   = 158; //250
    const int    JpegQuality = 100; //70
    const float  RetryDelay  = 1f;
    const int    ReqTimeout  = 10;

    static readonly string[] HazardKeywords =
    {
        "pedestrian", "person", "collision", "crash", "accident",
        "stop sign", "red light", "obstacle", "brake", "caution",
        "warning", "danger", "vehicle ahead",
        "cyclist", "emergency", "crossing", 
    };

    static readonly Color SafeColor   = Color.white;
    static readonly Color HazardColor = Color.red;

    // ── private state ──────────────────────────────────────────────────────
    RenderTexture renderTex;
    Texture2D     snapTex;

    private string aiCaption;
    private string lastCaptionText;

    // ── lifecycle ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (hoodCam == null)
            hoodCam = GetComponent<Camera>();

        EnsureUI();

        renderTex = new RenderTexture(TexWidth, TexHeight, 24);
        snapTex   = new Texture2D(TexWidth, TexHeight, TextureFormat.RGB24, false);
    }

    void Start()
    {
        StartCoroutine(CaptureLoop());
    }

    void OnDestroy()
    {
        if (renderTex != null)
        {
            renderTex.Release();
            Destroy(renderTex);
        }
        if (snapTex != null)
            Destroy(snapTex);
    }

    // ── UI bootstrap ───────────────────────────────────────────────────────

    void EnsureUI()
    {
        if (captionText != null)
            return;

        var canvasGO = new GameObject("DashcamHUD_Canvas");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGO.AddComponent<GraphicRaycaster>();

        // Semi-transparent dark banner across the top
        var panelGO   = new GameObject("HazardBanner");
        panelGO.transform.SetParent(canvasGO.transform, false);

        var panelRect         = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin   = new Vector2(0.05f, 0.90f);
        panelRect.anchorMax   = new Vector2(0.95f, 0.98f);
        panelRect.offsetMin   = Vector2.zero;
        panelRect.offsetMax   = Vector2.zero;

        var img   = panelGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);

        // TMP label centred inside the panel
        var labelGO   = new GameObject("HazardText");
        labelGO.transform.SetParent(panelGO.transform, false);

        var labelRect       = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12, 4);
        labelRect.offsetMax = new Vector2(-12, -4);

        captionText           = labelGO.AddComponent<TextMeshProUGUI>();
        captionText.alignment = TextAlignmentOptions.Center;
        captionText.fontSize  = 28;
        captionText.color     = SafeColor;
        captionText.text      = "Connecting to dashcam AI…";
    }

    // ── capture → request → display loop ──────────────────────────────────

    IEnumerator CaptureLoop()
    {
        while (true)
        {
            string b64 = CaptureFrame();
            yield return SendRequest(b64);
            // Response received (or error handled inside SendRequest).
            // Immediately loop — throughput is server-limited, no artificial delay.

            // Control the frequency          amt of time
            // yield return new WaitForSeconds(1f);
        }
    }

    string CaptureFrame()
    {
        // Temporarily redirect the hood cam into our RenderTexture without
        // permanently overwriting whatever targetTexture it had configured.
        RenderTexture prevRT = hoodCam.targetTexture;
        hoodCam.targetTexture = renderTex;
        hoodCam.Render();

        RenderTexture.active = renderTex;
        snapTex.ReadPixels(new Rect(0, 0, TexWidth, TexHeight), 0, 0);
        snapTex.Apply();
        RenderTexture.active = null;

        hoodCam.targetTexture = prevRT;

        return Convert.ToBase64String(snapTex.EncodeToJPG(JpegQuality));
    }

    IEnumerator SendRequest(string b64)
    {
        byte[] body = Encoding.UTF8.GetBytes(BuildPayload(b64));

        using var req = new UnityWebRequest(ServerUrl, "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();              //reaching out the the HTTP server to get the response .
        req.SetRequestHeader("Content-Type", "application/json");       //from the AI model
        req.timeout = ReqTimeout;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            SetCaption("[server unreachable]", isHazard: false);    // Show error in UI
            yield return new WaitForSeconds(RetryDelay);
            yield break;
        }

        //string caption = ParseCaption(req.downloadHandler.text); // TURINIG
        //SetCaption(caption, IsHazard(caption)); // TURINIG

        this.aiCaption = ParseCaption(req.downloadHandler.text); // TURINIG       // store the caption received from the AI model in aiCaption variable.
        SetCaption(this.aiCaption, IsHazard(this.aiCaption)); // TURINIG          // set the caption text and color based on whether it's a hazard or not.
    }

    // ── JSON helpers ───────────────────────────────────────────────────────

    // Build the payload as a string rather than serialising C# objects —
    // the base64 blob would just be copied anyway and this avoids an alloc.
    static string BuildPayload(string b64) =>
        "{\"model\":\"qwen2.5vl\",\"max_tokens\":16,\"temperature\":0.0001," +
        "\"messages\":[{\"role\":\"user\",\"content\":[" +
        "{\"type\":\"image_url\",\"image_url\":{\"url\":\"data:image/jpeg;base64," + b64 + "\"}}," +
        "{\"type\":\"text\",\"text\":\"You are the dash cam of the car. Describe hazards in under 10 words. be factual\"}" +

        "]}]}";

    // Unity JsonUtility deserialisation — properly typed, no string-searching.
    // Unknown fields in the response (timings, id, …) are silently ignored.
    [Serializable] class LlamaResponse { public LlamaChoice[] choices; }
    [Serializable] class LlamaChoice   { public LlamaMessage  message; }
    [Serializable] class LlamaMessage  { public string        content; }

    static string ParseCaption(string json)
    {
        try
        {
            var resp = JsonUtility.FromJson<LlamaResponse>(json);
            if (resp?.choices != null && resp.choices.Length > 0)
                return resp.choices[0].message?.content?.Trim() ?? "[empty response]";
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DashcamHazardHUD] JSON parse failed: {ex.Message}\nRaw: {json}");
        }
        return "[parse error]";
    }

    // ── hazard detection ───────────────────────────────────────────────────

    static bool IsHazard(string caption)
    {
        if (string.IsNullOrEmpty(caption))
            return false;

        string lower = caption.ToLowerInvariant();
        foreach (string kw in HazardKeywords)
            if (lower.Contains(kw))                 // If any of the hazard keywords are found in the caption, return true 
                return true;                            //making isHazard true.
                                                        
        return false;
    }

    void SetCaption(string text, bool isHazard)
    {
        if (captionText == null)
            return;

        // Skip redundant updates — same caption as last time, don't touch the UI.
        if (text == lastCaptionText)
            return;

        lastCaptionText = text;

        captionText.text  = text;
        captionText.color = isHazard ? HazardColor : SafeColor;   // if isHazard is true, set color to red, else set to white.
    }

    public string GetCaption() {
        return this.aiCaption;
    }
    
}
