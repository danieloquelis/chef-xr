using UnityEngine;

namespace Groq
{
    [CreateAssetMenu(menuName = "Groq/Config", fileName = "GroqConfig")]
    public class GroqConfig : ScriptableObject
    {
        public string apiKey;
        public string apiBaseUrl = "https://api.groq.com/openai/v1";
        public string model = "meta-llama/llama-4-scout-17b-16e-instruct";
        [Range(0f, 2f)] public float temperature = 1f;
        [Range(1, 8192)] public int maxCompletionTokens = 1024;
        public bool stream = false;
        public bool jsonMode = false;
    }
}