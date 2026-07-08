using System.ComponentModel;
using ModelContextProtocol.Server;

namespace mcp_server
{
    [McpServerToolType]
    public static class RecipeTools
    {
        private static MealieService _mealieService = null!;

        public static void Configure(MealieService mealieService)
        {
            _mealieService = mealieService;
        }

        [McpServerTool, Description("Retrieves a list of all available recipes from the server.")]
        public static async Task<string> GetAllRecipes()
        {
            return await _mealieService.GetAllRecipes();
        }

        [McpServerTool, Description("Retrieves only the names of all recipes from the server.")]
        public static async Task<string> GetAllRecipeNames()
        {
            return await _mealieService.GetAllRecipeNames();
        }

        [McpServerTool, Description("Retrieves today's meal plan.")]
        public static async Task<string> GetTodaysMeal()
        {
            return await _mealieService.GetTodaysMeal();
        }

        [McpServerTool, Description("Retrieves a recipe by its ID.")]
        public static async Task<string> GetRecipeById(string recipeId)
        {
            return await _mealieService.GetRecipeById(recipeId);
        }

        [McpServerTool, Description("Creates a recipe from a schema.org Recipe JSON document.")]
        public static async Task<string> CreateWithJSON([Description("The schema.org Recipe JSON document to pass to Mealie's create/html-or-json endpoint.")] string jsonSchema)
        {
            return await _mealieService.CreateWithJSONAsync(jsonSchema);
        }
    }
}