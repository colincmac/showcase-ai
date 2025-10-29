using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Showcase.Shared.AIExtensions;
using System.ClientModel;

namespace Showcase.GitHubCopilot.Extensions;
public class GitHubAgentFactory : IGitHubAgentFactory
{
    private readonly GitHubCopilotAgentOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAIToolRegistry _aiToolRegistry;

    public GitHubAgentFactory(IOptions<GitHubCopilotAgentOptions> options, IServiceProvider serviceProvider, IAIToolRegistry aiToolRegistry, IEnumerable<IGitHubAgentHandler> agentHandlers)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _aiToolRegistry = aiToolRegistry;
        foreach (var handler in agentHandlers)
        {
            var aiTools = handler.GetAITools();
            _aiToolRegistry.AddRange(aiTools);
        }
    }


    public IChatClient CreateAgent(string accessToken)
    {
        var completionsClient = new ChatClient(_options.ModelId, new ApiKeyCredential(accessToken), options: new OpenAI.OpenAIClientOptions() { Endpoint = new Uri(_options.ApiEndpoint) })
            .AsIChatClient();
        var innerClient = new ChatClientBuilder(completionsClient)
            .UseDistributedCache()
            .UseLogging()
            .UseFunctionInvocation()
            .Build();
        return new GitHubCopilotAgent(innerClient, _aiToolRegistry);
    }
}
