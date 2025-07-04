using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChefMR.AI
{
    /// <summary>
    /// Represents a kitchen ingredient detected by vision AI
    /// </summary>
    [Serializable]
    public class Ingredient
    {
        public string name;
        public string category;
        public string quantityEstimate;
        public string freshness;
        public float confidence;

        public override string ToString()
        {
            return $"{name} ({category}) - {quantityEstimate} - {freshness} (Confidence: {confidence:F2})";
        }
    }

    /// <summary>
    /// Represents a kitchen appliance detected by vision AI
    /// </summary>
    [Serializable]
    public class Appliance
    {
        public string name;
        public string type;
        public string status;
        public List<string> capabilities;
        public float confidence;

        public override string ToString()
        {
            return $"{name} ({type}) - {status} (Confidence: {confidence:F2})";
        }
    }

    /// <summary>
    /// Represents a voice command for timer operations
    /// </summary>
    [Serializable]
    public class TimerCommand
    {
        public string action; // start, stop, pause, cancel
        public int durationMinutes;
        public string timerName;
        public float confidence;

        public bool IsValid => !string.IsNullOrEmpty(action) && confidence > 0.5f;

        public override string ToString()
        {
            return $"Timer {action}: {durationMinutes} minutes - {timerName} (Confidence: {confidence:F2})";
        }
    }

    /// <summary>
    /// Represents a complete recipe generated from available ingredients
    /// </summary>
    [Serializable]
    public class Recipe
    {
        public string name;
        public int prepTimeMinutes;
        public int cookTimeMinutes;
        public string difficulty;
        public List<RecipeIngredient> ingredientsNeeded;
        public List<CookingStep> instructions;
        public List<string> tips;

        public int TotalTimeMinutes => prepTimeMinutes + cookTimeMinutes;

        public override string ToString()
        {
            return $"{name} - {difficulty} - {TotalTimeMinutes} minutes total";
        }
    }

    /// <summary>
    /// Represents an ingredient needed for a recipe
    /// </summary>
    [Serializable]
    public class RecipeIngredient
    {
        public string name;
        public string amount;

        public override string ToString()
        {
            return $"{amount} {name}";
        }
    }

    /// <summary>
    /// Represents a single cooking step in a recipe
    /// </summary>
    [Serializable]
    public class CookingStep
    {
        public int step;
        public string description;
        public int durationMinutes;
        public string applianceNeeded;

        public bool HasTimer => durationMinutes > 0;
        public bool RequiresAppliance => !string.IsNullOrEmpty(applianceNeeded);

        public override string ToString()
        {
            string result = $"Step {step}: {description}";
            if (HasTimer) result += $" ({durationMinutes} min)";
            if (RequiresAppliance) result += $" [Requires: {applianceNeeded}]";
            return result;
        }
    }

    /// <summary>
    /// Event args for ingredient recognition events
    /// </summary>
    public class IngredientRecognitionEventArgs : EventArgs
    {
        public List<Ingredient> Ingredients { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event args for appliance recognition events
    /// </summary>
    public class ApplianceRecognitionEventArgs : EventArgs
    {
        public List<Appliance> Appliances { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event args for voice command events
    /// </summary>
    public class VoiceCommandEventArgs : EventArgs
    {
        public string Transcription { get; set; }
        public TimerCommand TimerCommand { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event args for recipe generation events
    /// </summary>
    public class RecipeGenerationEventArgs : EventArgs
    {
        public Recipe Recipe { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Utility class for common AI operations
    /// </summary>
    public static class AIUtils
    {
        /// <summary>
        /// Check if confidence level is acceptable
        /// </summary>
        public static bool IsConfidenceAcceptable(float confidence, float threshold = 0.7f)
        {
            return confidence >= threshold;
        }

        /// <summary>
        /// Filter ingredients by confidence level
        /// </summary>
        public static List<Ingredient> FilterByConfidence(List<Ingredient> ingredients, float minConfidence = 0.7f)
        {
            return ingredients.FindAll(i => IsConfidenceAcceptable(i.confidence, minConfidence));
        }

        /// <summary>
        /// Filter appliances by confidence level
        /// </summary>
        public static List<Appliance> FilterByConfidence(List<Appliance> appliances, float minConfidence = 0.7f)
        {
            return appliances.FindAll(a => IsConfidenceAcceptable(a.confidence, minConfidence));
        }

        /// <summary>
        /// Get ingredients by category
        /// </summary>
        public static List<Ingredient> GetIngredientsByCategory(List<Ingredient> ingredients, string category)
        {
            return ingredients.FindAll(i => i.category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get appliances by type
        /// </summary>
        public static List<Appliance> GetAppliancesByType(List<Appliance> appliances, string type)
        {
            return appliances.FindAll(a => a.type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if recipe is suitable for available appliances
        /// </summary>
        public static bool IsRecipeFeasible(Recipe recipe, List<Appliance> availableAppliances)
        {
            var requiredAppliances = new HashSet<string>();
            foreach (var step in recipe.instructions)
            {
                if (step.RequiresAppliance)
                {
                    requiredAppliances.Add(step.applianceNeeded.ToLower());
                }
            }

            var availableApplianceNames = new HashSet<string>();
            foreach (var appliance in availableAppliances)
            {
                availableApplianceNames.Add(appliance.name.ToLower());
                foreach (var capability in appliance.capabilities)
                {
                    availableApplianceNames.Add(capability.ToLower());
                }
            }

            foreach (var required in requiredAppliances)
            {
                if (!availableApplianceNames.Contains(required))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculate recipe difficulty score based on steps and time
        /// </summary>
        public static float CalculateDifficultyScore(Recipe recipe)
        {
            float score = 0f;

            // Base score from number of steps
            score += recipe.instructions.Count * 0.2f;

            // Add score for total time
            score += recipe.TotalTimeMinutes * 0.01f;

            // Add score for appliance requirements
            int applianceCount = 0;
            foreach (var step in recipe.instructions)
            {
                if (step.RequiresAppliance) applianceCount++;
            }
            score += applianceCount * 0.3f;

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Format time duration in human-readable format
        /// </summary>
        public static string FormatDuration(int minutes)
        {
            if (minutes < 60)
            {
                return $"{minutes} min";
            }
            else
            {
                int hours = minutes / 60;
                int remainingMinutes = minutes % 60;
                if (remainingMinutes == 0)
                {
                    return $"{hours} hr";
                }
                else
                {
                    return $"{hours} hr {remainingMinutes} min";
                }
            }
        }

        /// <summary>
        /// Generate a simple cooking summary
        /// </summary>
        public static string GenerateRecipeSummary(Recipe recipe)
        {
            if (recipe == null) return "No recipe available";

            var summary = $"Recipe: {recipe.name}\n";
            summary += $"Difficulty: {recipe.difficulty}\n";
            summary += $"Total Time: {FormatDuration(recipe.TotalTimeMinutes)}\n";
            summary += $"Steps: {recipe.instructions.Count}\n";
            summary += $"Ingredients: {recipe.ingredientsNeeded.Count}";

            return summary;
        }
    }
}