using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// Pure network worker — no loop, no timing, no references to Capture or UI.
// EDashcamController calls SendRequest() whenever it wants a frame sent and
// gets the result back through the callback.
public class EDashcamAIClient : MonoBehaviour
{
    const string ServerUrl  = "http://localhost:8081/v1/chat/completions";
    const int    ReqTimeout = 10;

    public IEnumerator SendRequest(string b64, long frameId, Action<string, bool> onComplete)
    {
        byte[] body = Encoding.UTF8.GetBytes(BuildPayload(b64));

        using var req = new UnityWebRequest(ServerUrl, "POST");
        req.uploadHandler   = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = ReqTimeout;

        var stopwatch = Stopwatch.StartNew();
        yield return req.SendWebRequest();
        stopwatch.Stop();
        DashcamHistoryLogger.LogLatency(stopwatch.ElapsedMilliseconds, frameId);

        if (req.result != UnityWebRequest.Result.Success)
        {
            onComplete?.Invoke("[server unreachable]", true);
            yield break;
        }

        onComplete?.Invoke(ParseContent(req.downloadHandler.text), false);
    }

    // ── JSON helpers ───────────────────────────────────────────────────────

    // Build the payload as a string rather than serialising C# objects —
    // the base64 blob would just be copied anyway and this avoids an alloc.
    // Model: qwen3vl 4b
    static string BuildPayload(string b64) =>
        "{\"model\":\"qwen3vl\",\"max_tokens\":15,\"temperature\":0.0001," +
        "\"messages\":[{\"role\":\"user\",\"content\":[" +
        "{\"type\":\"image_url\",\"image_url\":{\"url\":\"data:image/jpeg;base64," + b64 + "\"}}," +
        "{\"type\":\"text\",\"text\":\"You are the dash cam of the car. Describe any " + 
        "important hazards in 5 words. Example: crosswalk, construction zone, stop sign, trafic light\"}" +
        "]}]}";

    [Serializable] class LlamaResponse { public LlamaChoice[] choices; }
    [Serializable] class LlamaChoice   { public LlamaMessage  message; }
    [Serializable] class LlamaMessage  { public string        content; }

    static string ParseContent(string json)
    {
        try
        {
            var resp = JsonUtility.FromJson<LlamaResponse>(json);
            if (resp?.choices != null && resp.choices.Length > 0)
                return resp.choices[0].message?.content?.Trim() ?? "[empty response]";
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EDashcamAIClient] JSON parse failed: {ex.Message}\nRaw: {json}");
        }
        return "[parse error]";
    }
}
