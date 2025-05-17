using System.Text;
using Microsoft.Extensions.Logging;

namespace mcp_server
{
    public class MealieService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<MealieService> _logger;

        public MealieService(IHttpClientFactory clientFactory, ILogger<MealieService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<string> GetAllRecipes()
        {
            var url = $"/api/recipes?orderDirection=desc&page=1&perPage=50&requireAllCategories=false&requireAllTags=false&requireAllTools=false&requireAllFoods=false";
            using var response = _clientFactory.CreateClient(nameof(MealieService)).GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            this._logger.LogError($"Error retrieving recipes: {response.StatusCode} - {response.ReasonPhrase}");
            return string.Empty;
        }

        public async Task<string> GetTodaysMeal()
        {
            var url = $"/api/households/mealplans/today";
            using var response = _clientFactory.CreateClient(nameof(MealieService)).GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            this._logger.LogError($"Error retrieving today's meal: {response.StatusCode} - {response.ReasonPhrase}");
            return string.Empty;
        }

        public async Task<string> GetRecipeById(string recipeId)
        {
            var url = $"/api/recipes/{recipeId}";
            using var response = _clientFactory.CreateClient(nameof(MealieService)).GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            this._logger.LogError(string.Format("Error retrieving recipe by ID: {0} - {1}", response.StatusCode,
                response.ReasonPhrase));
            return string.Empty;
        }

        public async Task<string> CreateWithJSONAsync(string jsonSchema)
        {
            try
            {

                var url = $"/api/recipes/create/html-or-json";
                var payload = $@"{{
                ""includeTags"": true,
                ""data"": {jsonSchema}
                }}";

                this._logger.LogError($"Payload: {payload}");
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _clientFactory.CreateClient(nameof(MealieService)).PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogError($"Error creating recipe: {response.StatusCode} - {response.ReasonPhrase}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating recipe: {ex.Message}");
                return string.Empty;
            }
        }
    }
}