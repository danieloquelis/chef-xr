using System;
using AI;
using Newtonsoft.Json;
using UnityEngine;

public class Inference : MonoBehaviour
{
    public Texture2D testPicture;

    private async void Start()
    {
        try
        {
            var encodedImage = GetReadableTexture(testPicture).EncodeToJPG();
            Debug.Log($"Running AI... {encodedImage}");
            var result = await AIManager.Instance.IdentifyKitchen(Convert.ToBase64String(encodedImage));
            Debug.Log($"Results: {JsonConvert.SerializeObject(result, Formatting.Indented)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Something went wrong: {e}");
        }
    }
    
    private Texture2D GetReadableTexture(Texture2D source)
    {
        var tmp = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, tmp);
        var previous = RenderTexture.active;
        RenderTexture.active = tmp;

        var readable = new Texture2D(source.width, source.height, TextureFormat.RGB24, false);
        readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        return readable;
    }
}