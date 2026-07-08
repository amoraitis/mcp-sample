# MCP Sample Project

This repository contains a sample implementation of a Model Context Protocol (MCP) server for recipe management, using .NET 10, Serilog, and Mealie API integration.

## Features
- [x] Retrieve all recipes from a Mealie server
- [x] Stream all recipe names from a Mealie server
- [x] Get today's meal plan
- [x] Fetch a recipe by its ID - helps when the copilot has already fetched one from the previous queries
- [x] Create a recipe using a schema.org Recipe JSON document
- [x] Create a recipe from a recipe webpage URL
- [x] Logging with Serilog

## Project Structure
- `mcp-server/` - Main MCP server implementation
  - `Program.cs` - Entry point and DI setup
  - `MealieService.cs` - Service for Mealie API interactions
  - `MealieOptions.cs` - Configuration options for Mealie
  - `RecipeTools.cs` - MCP tool definitions
- `tests/` - Unit tests using NUnit and Moq

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Access to a Mealie server (for real API calls)

## Getting Started
1. **Clone the repository:**
   ```sh
   git clone https://github.com/amoraitis/recipe-mcp.git
   cd recipe-mcp
   ```
2. **Configure Mealie options:**
   Set environment variables or update your configuration for:
   - `mealie:BaseUrl` (e.g., `http://localhost:9925`)
   - `mealie:Token` (your Mealie API token)

3. **Build the project:**
   ```sh
   dotnet build
   ```
4. **Run the server:**
   ```sh
   dotnet run --project mcp-server
   ```

## MCP tools
- `GetAllRecipes` - retrieves all recipes from Mealie.
- `GetAllRecipeNames` - streams recipe names from Mealie using `IAsyncEnumerable<string>`.
- `GetTodaysMeal` - retrieves today's meal plan.
- `GetRecipeById` - retrieves a recipe by ID.
- `CreateWithJSON` - creates a recipe from a schema.org Recipe JSON document through Mealie's `create/html-or-json` endpoint.
- `CreateWithUrl` - creates a recipe from a recipe webpage URL through Mealie's `create/html-or-json` endpoint.

## VS Code integration

Add `.vscode/mcp.json`:

```json
{
  "inputs": [],
  "servers": {
    "RecipeMCP": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "{path-to-repo}\\mcp-server.csproj",
        "--",
        "--mealie:token",
        "{MealieToken}",
        "--mealie:baseUrl",
        "http://localhost:9925"
      ]
    }
  }
}

```

## Running Tests
```sh
dotnet test
```

## Extending
- Add new tools to `RecipeTools.cs` following the `[McpServerTool]` pattern.
- Add new services or integrations as needed.