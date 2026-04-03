using Hermes.Agent.Core;
using Hermes.Agent.LLM;
using Hermes.Agent.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;

var rootCommand = new RootCommand("Hermes.C# AI Agent CLI");

var chatCommand = new Command("chat", "Send a message to Hermes")
{
    new Argument<string>("message", "Message to send"),
};
chatCommand.SetHandler(async (InvocationContext ctx) =>
{
    var message = ctx.ParseResult.GetValueForArgument(chatCommand.Arguments[0] as Argument<string>);
    var services = new ServiceCollection();
    
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
    
    // TODO: Load config from ~/.hermes-cs/config.yaml
    var config = new LlmConfig
    {
        Provider = "custom",
        Model = "minimax-m2.7:cloud",
        BaseUrl = "http://127.0.0.1:11434/v1",
        ApiKey = "no-key-required"
    };
    
    services.AddSingleton<IChatClient>(new OpenAiClient(config, new HttpClient()));
    services.AddSingleton<IAgent, Agent>();
    services.AddSingleton<ITool, TerminalTool>();
    
    var serviceProvider = services.BuildServiceProvider();
    var agent = serviceProvider.GetRequiredService<IAgent>();
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    var session = new Session
    {
        Id = Guid.NewGuid().ToString(),
        Platform = "cli",
        UserId = Environment.UserName,
    };
    
    foreach (var tool in serviceProvider.GetServices<ITool>())
        agent.RegisterTool(tool);
    
    try
    {
        logger.LogInformation("Sending message: {Message}", message);
        var response = await agent.ChatAsync(message, session, CancellationToken.None);
        Console.WriteLine(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Chat failed");
        Environment.Exit(1);
    }
});

rootCommand.AddCommand(chatCommand);
return await rootCommand.InvokeAsync(args);
