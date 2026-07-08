using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace mcp_server
{
    public class MealieService
    {
        private const int RecipePageSize = 100;

        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<MealieService> _logger;

        public MealieService(IHttpClientFactory clientFactory, ILogger<MealieService> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<string> GetAllRecipes()
        {
            try
            {
                var recipes = new List<JsonElement>();
                var page = 1;
                int? total = null;

                while (true)
                {
                    var pageJson = await GetRecipesPageAsync(page, RecipePageSize);
                    if (string.IsNullOrWhiteSpace(pageJson))
                    {
                        return string.Empty;
                    }

                    using var document = JsonDocument.Parse(pageJson);

                    if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                    {
                        return document.RootElement.GetRawText();
                    }

                    var itemsOnPage = 0;
                    foreach (var item in items.EnumerateArray())
                    {
                        recipes.Add(item.Clone());
                        itemsOnPage++;
                    }

                    if (total is null && document.RootElement.TryGetProperty("total", out var totalElement) && totalElement.TryGetInt32(out var parsedTotal))
                    {
                        total = parsedTotal;
                    }

                    if (itemsOnPage == 0 || (total.HasValue && recipes.Count >= total.Value))
                    {
                        break;
                    }

                    page++;
                }

                return JsonSerializer.Serialize(new
                {
                    items = recipes,
                    total = recipes.Count,
                    page = 1,
                    perPage = recipes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipes");
                return string.Empty;
            }
        }

        public async IAsyncEnumerable<string> GetAllRecipeNames([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var page = 1;
            var processedRecipes = 0;
            int? total = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var recipeNamesPage = await GetRecipeNamesPageAsync(page, RecipePageSize, cancellationToken);
                if (recipeNamesPage is null || recipeNamesPage.Value.ItemsOnPage == 0)
                {
                    yield break;
                }

                foreach (var name in recipeNamesPage.Value.Names)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return name;
                }

                processedRecipes += recipeNamesPage.Value.ItemsOnPage;
                total ??= recipeNamesPage.Value.Total;
                if (total.HasValue && processedRecipes >= total.Value)
                {
                    yield break;
                }

                page++;
            }
        }

        public async Task<string> GetTodaysMeal()
        {
            var url = "/api/households/mealplans/today";
            using var response = await _clientFactory.CreateClient(nameof(MealieService)).GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            _logger.LogError("Error retrieving today's meal: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return string.Empty;
        }

        public async Task<string> GetRecipeById(string recipeId)
        {
            var url = $"/api/recipes/{recipeId}";
            using var response = await _clientFactory.CreateClient(nameof(MealieService)).GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            _logger.LogError("Error retrieving recipe by ID: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return string.Empty;
        }

        public async Task<string> CreateWithJSONAsync(string jsonSchema)
        {
            try
            {
                var url = "/api/recipes/create/html-or-json";
                var payload = JsonSerializer.Serialize(new
                {
                    includeTags = true,
                    data = jsonSchema
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _clientFactory.CreateClient(nameof(MealieService)).PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                _logger.LogError("Error creating recipe: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe");
                return string.Empty;
            }
        }

        private async Task<string> GetRecipesPageAsync(int page, int perPage, CancellationToken cancellationToken = default)
        {
            var url = $"/api/recipes?orderDirection=desc&page={page}&perPage={perPage}&requireAllCategories=false&requireAllTags=false&requireAllTools=false&requireAllFoods=false";
            using var response = await _clientFactory.CreateClient(nameof(MealieService)).GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }

            _logger.LogError("Error retrieving recipes: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return string.Empty;
        }

        private async Task<(List<string> Names, int ItemsOnPage, int? Total)?> GetRecipeNamesPageAsync(int page, int perPage, CancellationToken cancellationToken)
        {
            try
            {
                var pageJson = await GetRecipesPageAsync(page, perPage, cancellationToken);
                if (string.IsNullOrWhiteSpace(pageJson))
                {
                    return null;
                }

                using var document = JsonDocument.Parse(pageJson);
                if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var names = new List<string>();
                var itemsOnPage = 0;
                foreach (var recipe in items.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    itemsOnPage++;

                    if (recipe.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                    {
                        var name = nameElement.GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            names.Add(name);
                        }
                    }
                }

                int? total = null;
                if (document.RootElement.TryGetProperty("total", out var totalElement) && totalElement.TryGetInt32(out var parsedTotal))
                {
                    total = parsedTotal;
                }

                return (names, itemsOnPage, total);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe names");
                return null;
            }
        }
    }
}