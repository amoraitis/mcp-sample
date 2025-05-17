using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;

namespace mcp_server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                 .WriteTo.File($"{Path.GetTempPath()}Logs/mcp/app-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            Log.Logger.Information("Starting Recipe MCP server...");
            
            var builder = Host.CreateApplicationBuilder(args);

            // Add configuration for environment variables
            builder.Configuration.AddEnvironmentVariables();
            builder.Services.AddOptions<MealieOptions>()
                .Bind(builder.Configuration.GetSection("mealie"));

            builder.Services.AddHttpClient(nameof(MealieService), (serviceProvider, client) =>
            {
                var mealieOptions = serviceProvider.GetRequiredService<IOptions<MealieOptions>>();
                client.BaseAddress = new Uri(mealieOptions.Value.BaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", mealieOptions.Value.Token);
            });

            builder.Services.AddSerilog();

            builder.Services
                .AddSingleton<MealieService>()
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            var app = builder.Build();
            
            RecipeTools.Configure(app.Services.GetRequiredService<MealieService>());
            await app.RunAsync();
        }
    }
}
