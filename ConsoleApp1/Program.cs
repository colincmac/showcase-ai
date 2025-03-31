#pragma warning disable OPENAI002
using Azure.AI.OpenAI;
using Azure.Identity;
using ConsoleApp1;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenAI.RealtimeConversation;
using Showcase.AI.Voice.SemanticKernel;
using Showcase.AI.Voice.Tools;
using Showcase.AudioOrchestration;
using Showcase.Shared.AIExtensions.Realtime;
using System.ClientModel;
using System.IO;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine("Hello, World!");
var kernel = Kernel.CreateBuilder().Build();
var loggerFactory = new LoggerFactory();
// Add Appsettings.json into configuration

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .Build();
var azureOpenAIConfig = configuration.GetRequiredSection(AzureOpenAISettings.SectionName).Get<AzureOpenAISettings>();

await StartAsync();

async Task StartAsync()
{
    RealtimeConversationClient client = GetConfiguredClientForAzureOpenAIWithKey();


    var cts = new CancellationTokenSource();
    var options = new RealtimeSessionOptions()
    {
        ToolChoice = ConversationToolChoice.CreateAutoToolChoice(),
        Tools = [AIFunctionFactory.Create(() => { }, name: "user_wants_to_finish_conversation", description: "Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.")],
        InputTranscriptionOptions = new()
        {
            Model = "whisper-1",
        },
    };
    var localParticipant = new TestParticipant();
    var aiParticipant = new OpenAIRealtimeAgent(client, options, loggerFactory, "aiLocal", "aiLocal");

    localParticipant.SubscribeTo(aiParticipant);
    aiParticipant.SubscribeTo(localParticipant);
    await Task.WhenAll(localParticipant.StartResponseAsync(cts.Token), aiParticipant.StartResponseAsync(cts.Token));

}


RealtimeConversationClient GetConfiguredClientForAzureOpenAIWithKey()
{
    AzureOpenAIClient aoaiClient = new(new Uri(azureOpenAIConfig.Endpoint), new ApiKeyCredential(azureOpenAIConfig.Key));
    return aoaiClient.GetRealtimeConversationClient(azureOpenAIConfig.DeploymentName);
}

