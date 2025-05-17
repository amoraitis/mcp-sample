using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;

namespace mcp_server
{
    [McpServerToolType]
    public static class RecipeTools
    {
        private static MealieService _mealieService;

        public static void Configure(MealieService mealieService)
        {
            _mealieService = mealieService;
        }

        [McpServerTool, Description("Retrieves a list of all available recipes from the server.")]
        public static async Task<string> GetAllRecipes()
        {
            return await _mealieService.GetAllRecipes();
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

        [McpServerTool, Description("Creates a recipe using a JSON, the data are included in the json.")]
        public static async Task<string> CreateWithJSON([Description("The schema.org/recipe final json that will be passed as the Mealie createhtmlorjson.")]string jsonSchema)
        {
            return await _mealieService.CreateWithJSONAsync(jsonSchema);
        }
    }
}
