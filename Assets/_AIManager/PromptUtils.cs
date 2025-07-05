using UnityEngine;

namespace AI
{
    public class PromptUtils
    {
        public enum PromptType
        {
            KitchenScannerSystem,
            KitchenScannerUser,
            RecipeGeneratorSystem
            // Add more as needed
        }
        private const string PromptFolder = "Prompts";

        public static string Load(PromptType type)
        {
            var fileName = type.ToString();
            var path = $"{PromptFolder}/{fileName}";

            var promptAsset = Resources.Load<TextAsset>(path);

            if (promptAsset) return promptAsset.text;
            
            Debug.LogError($"Prompt file not found at Resources/{path}.txt");
            return string.Empty;
        }
    }
}