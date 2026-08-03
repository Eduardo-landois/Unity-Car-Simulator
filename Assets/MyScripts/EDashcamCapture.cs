using System;
using UnityEngine;

/// Owns the hood camera + render textures. Its only job: turn the current
/// frame into a base64 JPEG string on demand.
public class EDashcamCapture : MonoBehaviour
{
    [SerializeField] Camera hoodCam;

    const int TexWidth    = 336;            //280 old ;
    const int TexHeight   = 252;            //158 old ;
    const int JpegQuality = 100;

    RenderTexture renderTex;
    Texture2D     snapTex;

    void Awake()
    {
        if (hoodCam == null)
            hoodCam = GetComponent<Camera>();

        renderTex = new RenderTexture(TexWidth, TexHeight, 24);
        snapTex   = new Texture2D(TexWidth, TexHeight, TextureFormat.RGB24, false);
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

    public string CaptureFrameBase64()
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
}
