using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using ChefMR.AI;

public class ApplianceModule : MonoBehaviour
{
    [Header("Test Image")]
    public Texture2D testImage;

    [TextArea]
    public string appliancePrompt = "Identify all kitchen appliances and return their status and capabilities.";

    [TextArea]
    public string jsonSchema = @"{
        ""appliances"": [
            {
                ""name"": ""microwave"",
                ""type"": ""microwave"",
                ""status"": ""on"",
                ""capabilities"": [""heating""],
                ""confidence"": 0.95
            }
        ]
    }";

    [Button("Run Appliance Scan")]
    public void TestApplianceScan()
    {
        ScanAppliances(testImage, appliancePrompt, jsonSchema);
    }


//public void Recognize(Texture2D image)
//{
//    ChefAIFramework.Instance.RecognizeAppliances(image, (results) =>
//    {
//        foreach (var app in results)
//            Debug.Log($"🔌 Appliance: {app.name}, Status: {app.status}, Confidence: {app.confidence}");
//    });
//}

//public void RecognizeWithSafety(Texture2D image, bool includeSafety)
//{
//    ChefAIFramework.Instance.RecognizeAppliancesWithSafety(image, includeSafety, (results) =>
//    {
//        Debug.Log($"🛡️ Appliance scan found {results.Count} items.");
//    });
//}

    public void ScanAppliances(Texture2D image, string prompt, string jsonSchema)
    {
        ChefAIFramework.Instance.AnalyzeImageWithJSON(image, prompt, jsonSchema, "appliances", (jsonResponse) =>
        {
            if (jsonResponse == null)
            {
                Debug.LogWarning("Appliance scan failed.");
                return;
            }

            var appliances = ChefAIFramework.Instance.ParseAppliancesFromJSON(jsonResponse);
            foreach (var app in appliances)
                Debug.Log($"🔌 {app.name} ({app.status}) - {app.confidence}");

            // TODO: Visualize appliance overlays, anchor to surfaces, etc.
        });
    }

}