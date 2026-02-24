using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Connectors.OpenRouter.Extensions;
using SemanticKernel.Connectors.OpenRouter.Models;

namespace OpenRouter.Examples;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        var apiKey = config["ApiKey"] ?? throw new Exception("API key not found in user secrets.");

        var builder = Kernel.CreateBuilder().AddOpenRouterChatCompletion(
            apiKey: apiKey,
            modelId: "anthropic/claude-sonnet-4.6"
        );
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

        var kernel = builder.Build();
        kernel.Plugins.AddFromType<WeatherPlugin>("Weather");
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var settings = new OpenRouterExecutionSettings
        {
            Models = ["anthropic/claude-sonnet-4.6"],
            Provider = new { order = new[] { "amazon-bedrock" } },
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        Console.WriteLine("Semantic Kernel connected to OpenRouter!");

        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful assistant that can use plugins to assist with tasks.");
        history.AddUserMessage("What is the weather?");
        var result = await chatService.GetChatMessageContentsAsync(history, settings, kernel);
        foreach (var r in result)
        {
            history.AddMessage(r.Role, r.Content ?? string.Empty);
            Console.WriteLine(r.Role + " > " + r.Content);
        }

        Console.WriteLine("Done");
    }
}

public class WeatherPlugin
{
    [KernelFunction("get_weather"), Description("Gets the current weather conditions")]
    public async Task<string> GetWeather()
    {
        return "It's sunny today!";
    }
}