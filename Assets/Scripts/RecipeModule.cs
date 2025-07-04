using System.Collections.Generic;
using UnityEngine;
using ChefMR.AI;
using NaughtyAttributes;

public class RecipeModule : MonoBehaviour
{
    [Header("Test Inputs")]
    public List<Ingredient> testIngredients;
    public List<Appliance> testAppliances;

    [TextArea]
    public string dietaryRestrictions = "vegan, gluten-free";

    [Button("Generate Recipe")]
    private void testRecipeGeneration() {
        GenerateRecipe(testIngredients, testAppliances);
    }

    [Button("Generate Recipe With Diet")]
    private void testDietRecipeGeneration()
    {
        var restrictions = new List<string>(dietaryRestrictions.Split(','));
        GenerateWithDiet(testIngredients, testAppliances, restrictions);
    }

    public void GenerateRecipe(List<Ingredient> ingredients, List<Appliance> appliances)
    {
        ChefAIFramework.Instance.GenerateRecipe(ingredients, appliances, (recipe) =>
        {
            if (recipe == null)
            {
                Debug.LogWarning("🍳 Recipe generation failed.");
                return;
            }

            Debug.Log($"🍽️ {recipe.name} ({recipe.difficulty})");
            foreach (var step in recipe.instructions)
                Debug.Log($"➡️ Step {step.step}: {step.description} using {step.applianceNeeded}");

            // TODO: Display steps in MR, add timers, voice narration, etc.
        });
    }

    public void GenerateWithDiet(List<Ingredient> ingredients, List<Appliance> appliances, List<string> restrictions)
    {
        ChefAIFramework.Instance.GenerateRecipeWithDiet(ingredients, appliances, restrictions, (recipe) =>
        {
            Debug.Log($"🥗 Diet Recipe: {recipe?.name ?? "Unknown"}");
        });
    }
}