using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace mcp_server.Tests
{
    [TestFixture]
    public class MealieServiceTests
    {
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<ILogger<MealieService>> _mockLogger;
        private MealieService _mealieService;

        [SetUp]
        public void SetUp()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockLogger = new Mock<ILogger<MealieService>>();
            _mealieService = new MealieService(_mockHttpClientFactory.Object, _mockLogger.Object);
        }

        private HttpClient CreateMockHttpClient(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK, Action<HttpRequestMessage>? inspectRequest = null)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, _) => inspectRequest?.Invoke(request))
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent)
                });

            var client = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            return client;
        }

        [Test]
        public async Task GetAllRecipes_ReturnsRecipes_WhenSuccessful()
        {
            // Arrange
            var client = CreateMockHttpClient("{\"items\":[{\"id\":1,\"name\":\"Recipe1\"}],\"total\":1}");
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient(nameof(MealieService)))
                .Returns(client);

            // Act
            var result = await _mealieService.GetAllRecipes();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Recipe1"));
        }

        [Test]
        public async Task GetAllRecipeNames_StreamsRecipeNames_WhenSuccessful()
        {
            // Arrange
            var client = CreateMockHttpClient("{\"items\":[{\"id\":1,\"name\":\"Recipe1\"},{\"id\":2,\"name\":\"Recipe2\"}],\"total\":2}");
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient(nameof(MealieService)))
                .Returns(client);

            // Act
            var names = new List<string>();
            await foreach (var name in _mealieService.GetAllRecipeNames())
            {
                names.Add(name);
            }

            // Assert
            Assert.That(names, Is.EqualTo(new[] { "Recipe1", "Recipe2" }));
        }

        [Test]
        public async Task GetTodaysMeal_ReturnsMeal_WhenSuccessful()
        {
            // Arrange
            var client = CreateMockHttpClient("{\"meal\":\"Lunch\"}");
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient(nameof(MealieService)))
                .Returns(client);

            // Act
            var result = await _mealieService.GetTodaysMeal();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Lunch"));
        }

        [Test]
        public async Task GetRecipeById_ReturnsRecipe_WhenSuccessful()
        {
            // Arrange
            var recipeId = "123";
            var client = CreateMockHttpClient("{\"id\":123,\"name\":\"Recipe123\"}");
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient(nameof(MealieService)))
                .Returns(client);

            // Act
            var result = await _mealieService.GetRecipeById(recipeId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("Recipe123"));
        }

        [Test]
        public async Task CreateWithJSONAsync_SendsSchemaAsJsonString_WhenSuccessful()
        {
            // Arrange
            var jsonSchema = "{\"name\":\"New Recipe\"}";
            string? requestBody = null;
            Uri? requestUri = null;
            var client = CreateMockHttpClient("{\"success\":true}", inspectRequest: request =>
            {
                requestUri = request.RequestUri;
                requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            });
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient(nameof(MealieService)))
                .Returns(client);

            // Act
            var result = await _mealieService.CreateWithJSONAsync(jsonSchema);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("success"));
            Assert.That(requestUri?.AbsolutePath, Is.EqualTo("/api/recipes/create/html-or-json"));
            Assert.That(requestBody, Is.Not.Null);

            using var document = JsonDocument.Parse(requestBody!);
            Assert.That(document.RootElement.GetProperty("includeTags").GetBoolean(), Is.True);
            Assert.That(document.RootElement.GetProperty("data").GetString(), Is.EqualTo(jsonSchema));
        }

        [Test]
        public async Task CreateWithUrlAsync_SendsUrlStreamEndpointPayload_WhenSuccessful()
        {
            // Arrange
            var recipeUrl = "https://example.com/recipes/pasta";
            string? requestBody = null;
            Uri? requestUri = null;
            var client = CreateMockHttpClient("{\"success\":true}", inspectRequest: request =>
            {
                requestUri = request.RequestUri;
                requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            });
            _mockHttpClientFactory
                .Setup(factory => factory.CreateClient(nameof(MealieService)))
                .Returns(client);

            // Act
            var result = await _mealieService.CreateWithUrlAsync(recipeUrl);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("success"));
            Assert.That(requestUri?.AbsolutePath, Is.EqualTo("/api/recipes/create/url/stream"));
            Assert.That(requestBody, Is.Not.Null);

            using var document = JsonDocument.Parse(requestBody!);
            Assert.That(document.RootElement.GetProperty("includeCategories").GetBoolean(), Is.True);
            Assert.That(document.RootElement.GetProperty("includeTags").GetBoolean(), Is.True);
            Assert.That(document.RootElement.GetProperty("url").GetString(), Is.EqualTo(recipeUrl));
        }
    }
}