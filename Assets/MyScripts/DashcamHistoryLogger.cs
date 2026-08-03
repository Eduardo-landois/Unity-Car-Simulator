using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Writes exactly one CSV row per captured frame, filled in as that frame's
// snapshot / AI caption / latency / UI-display events arrive — so a
// session's log.csv can be read frame-by-frame (snapshot, AI response,
// latency, and whether it was shown all in the same row) instead of
// filtering/joining separate event rows by frameId by hand.
// Files land under Application.persistentDataPath/DashcamLogs/<session>/ —
// one log.csv plus one .jpg per snapshot.
public static class DashcamHistoryLogger
{
    // Flip off to stop writing snapshots/log rows entirely. Toggling back on
    // resumes into the same session folder rather than starting a new one.
    public static bool Enabled = true;

    class FrameRecord
    {
        public string captureTimestamp = "";
        public string snapshotFile     = "";
        public string aiCaption        = "";
        public string aiLatencyMs      = "";
        public bool   shownOnUI;
        public bool   pedestrianGroundTruth;
        public bool   animalGroundTruth;
    }

    static string sessionDir;
    static string logPath;
    static readonly List<long> frameOrder = new();
    static readonly Dictionary<long, FrameRecord> frames = new();

    // Ground-truth icons toggle independently of the capture cadence, so they
    // just tag whichever frame was most recently captured rather than
    // needing their own frameId passed in from the caller.
    static long latestFrameId;

    // Ground truth only gets logged on a change (see LogPedestrianGroundTruth
    // / LogAnimalGroundTruth), but a hazard usually stays visible across many
    // captured frames in a row. These remember the current state so every
    // new frame row starts out correctly instead of defaulting back to
    // false until the next transition.
    static bool currentPedestrianGroundTruth;
    static bool currentAnimalGroundTruth;

    static void EnsureSession()
    {
        if (sessionDir != null)
            return;

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        sessionDir = Path.Combine(Application.persistentDataPath, "DashcamLogs", stamp);
        Directory.CreateDirectory(sessionDir);
        logPath = Path.Combine(sessionDir, "log.csv");

        Debug.Log($"[DashcamHistoryLogger] Logging this session to {sessionDir}");
    }

    static FrameRecord GetRecord(long frameId)
    {
        if (!frames.TryGetValue(frameId, out FrameRecord record))
        {
            record = new FrameRecord
            {
                pedestrianGroundTruth = currentPedestrianGroundTruth,
                animalGroundTruth     = currentAnimalGroundTruth,
            };
            frames[frameId] = record;
            frameOrder.Add(frameId);
        }
        return record;
    }

    // Rewrites the whole CSV from the in-memory table so every frame this
    // session has exactly one, always-up-to-date row. A full rewrite per
    // update is more I/O than pure appending, but sessions are short enough
    // (hundreds to low thousands of frames) that this stays cheap, and it's
    // far easier to read than an append-only event stream.
    static void Flush()
    {
        var sb = new StringBuilder();
        sb.AppendLine("frameId,captureTimestamp,snapshotFile,aiCaption,aiLatencyMs,shownOnUI,pedestrianGroundTruth,animalGroundTruth");

        foreach (long frameId in frameOrder)
        {
            FrameRecord r = frames[frameId];
            sb.AppendLine(string.Join(",",
                frameId.ToString(),
                Csv(r.captureTimestamp),
                Csv(r.snapshotFile),
                Csv(r.aiCaption),
                Csv(r.aiLatencyMs),
                r.shownOnUI ? "TRUE" : "FALSE",
                r.pedestrianGroundTruth ? "TRUE" : "FALSE",
                r.animalGroundTruth ? "TRUE" : "FALSE"));
        }

        File.WriteAllText(logPath, sb.ToString());
    }

    static string Csv(string value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";

    // Called with the base64 JPEG string at the moment a frame is captured.
    // Decodes it back to bytes, saves the image to disk (filename carries
    // the frameId so it sorts/matches naturally), and starts that frame's row.
    public static void LogSnapshot(string jpegBase64, long frameId)
    {
        if (!Enabled)
            return;

        EnsureSession();
        byte[] jpegBytes = Convert.FromBase64String(jpegBase64);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string filename = $"frame_{frameId:D6}_{timestamp}.jpg";
        File.WriteAllBytes(Path.Combine(sessionDir, filename), jpegBytes);

        FrameRecord record = GetRecord(frameId);
        record.captureTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        record.snapshotFile     = filename;
        latestFrameId = frameId;
        Flush();
    }

    // Called with the caption text as soon as the AI response comes back.
    public static void LogCaption(string caption, long frameId)
    {
        if (!Enabled)
            return;

        EnsureSession();
        GetRecord(frameId).aiCaption = caption;
        Flush();
    }

    // Called whenever the on-screen caption actually changes to this frame's text.
    public static void LogUIDisplay(string text, long frameId)
    {
        if (!Enabled)
            return;

        EnsureSession();
        GetRecord(frameId).shownOnUI = true;
        Flush();
    }

    // Called by EDashcamBottomUI whenever its pedestrian ground-truth icon
    // actually changes state (lights up or turns off), tagging whichever
    // frame was most recently captured.
    public static void LogPedestrianGroundTruth(bool visible)
    {
        if (!Enabled)
            return;

        EnsureSession();
        currentPedestrianGroundTruth = visible;
        GetRecord(latestFrameId).pedestrianGroundTruth = visible;
        Flush();
    }

    // Called by EDashcamAnimalUI whenever its animal ground-truth icon
    // actually changes state (lights up or turns off), tagging whichever
    // frame was most recently captured.
    public static void LogAnimalGroundTruth(bool visible)
    {
        if (!Enabled)
            return;

        EnsureSession();
        currentAnimalGroundTruth = visible;
        GetRecord(latestFrameId).animalGroundTruth = visible;
        Flush();
    }

    // Round-trip time for one AI request, in milliseconds — lets before/after
    // prompt changes be compared from the CSV instead of eyeballing it.
    public static void LogLatency(long elapsedMs, long frameId)
    {
        if (!Enabled)
            return;

        EnsureSession();
        GetRecord(frameId).aiLatencyMs = elapsedMs.ToString();
        Flush();
    }
}
