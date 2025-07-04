using System.Collections.Generic;
using UnityEngine;
using ChefMR.AI;
using NaughtyAttributes;

public class IngredientModule : MonoBehaviour
{
    [Header("Test Settings")]
    public Texture2D testImage;

    [TextArea]
    public string testPrompt = @"You are a vision model that analyzes kitchen scenes and returns structured JSON of the ingredients found. 
        Follow this exact schema. Do not include any commentary, explanation, or non-JSON formatting.
        Only output a single valid JSON object compatible with Newtonsoft.Json in Unity.
        Return a key ""status"": ""ok"" at the end to confirm response was rendered correctly.

        Return for each ingredient:
        - 'name': short common name (e.g. 'tomato', 'egg')
        - 'freshness': one of ['fresh', 'stale', 'spoiled', 'unknown']
        - 'confidence': float between 0.0 and 1.0
        - 'bounding_box': array of 4 float values in normalized screen space [xMin, yMin, xMax, yMax]

        Ensure bounding_box contains only numeric values. Use dot notation for floats (e.g. 0.82 not 82%).

        Example schema:
        {
          ""ingredients"": [
            {
              ""name"": ""tomato"",
              ""freshness"": ""fresh"",
              ""bounding_box"": [0.42, 0.21, 0.55, 0.47],
              ""confidence"": 0.95
            },
            {
              ""name"": ""potato"",
              ""freshness"": ""stale"",
              ""bounding_box"": [0.15, 0.28, 0.33, 0.39],
              ""confidence"": 0.87
            }
          ]
        }";

    [TextArea]
    public string jsonSchema = @"{
        ""ingredients"": [
            {
                ""name"": ""tomato"",
                ""freshness"": ""fresh"",
                ""bounding_box"": [0.1, 0.2, 0.3, 0.4],
                ""confidence"": 0.95
            }
        ]
    }";


    [Button("Run Ingredient Scan")]
    private void testIngredientScan()
    {
        ScanIngredients(testImage, testPrompt, jsonSchema);
    }
    

    //public void Recognize(Texture2D image)
    //{
    //    ChefAIFramework.Instance.RecognizeIngredients(image, (results) =>
    //    {
    //        foreach (var ing in results)
    //            Debug.Log($"🧅 Ingredient: {ing.name}, Freshness: {ing.freshness}, Confidence: {ing.confidence}");
    //    });
    //}

    //public void RecognizeCustom(Texture2D image, string prompt)
    //{
    //    ChefAIFramework.Instance.RecognizeIngredientsCustom(image, prompt, (results) =>
    //    {
    //        Debug.Log($"🔍 Custom scan returned {results.Count} ingredients.");
    //    });
    //}

    public void ScanIngredients(Texture2D image, string prompt, string jsonSchema)
    {
        if (testImage == null)
        {
            Debug.LogWarning("Test image not assigned.");
            return;
        }

        ChefAIFramework.Instance.AnalyzeImageWithJSON(image, prompt, jsonSchema, "ingredients", (jsonResponse) =>
        {
            if (jsonResponse == null)
            {
                Debug.LogWarning("Ingredient scan failed.");
                return;
            }

            if (jsonResponse["status"]?.ToString() != "ok")
            {
                Debug.LogWarning("Groq reported internal formatting issues.");
            }

            // You can use the built-in parser or write your own
            var ingredients = ChefAIFramework.Instance.ParseIngredientsFromJSON(jsonResponse);
            Debug.Log($"Raw Response: {jsonResponse}");
            foreach (var ing in ingredients)
                Debug.Log($"[Test] 🧅 {ing.name} ({ing.freshness}) - {ing.confidence}");

            // TODO: Visualize bounding boxes, anchor ingredients, etc.
        });
    }
}