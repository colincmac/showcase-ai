using Azure.AI.OpenAI;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using Showcase.Shared.AIExtensions.Realtime;
using Showcase.VoiceRagAgent;
using OpenAI.RealtimeConversation;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using Microsoft.Extensions.Options;
using Azure.Communication;
using Showcase.AudioOrchestration;
using Showcase.Shared.AIExtensions;
using Showcase.AI.Voice.Tools;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("OpenAI.Experimental.EnableOpenTelemetry", true);

builder.AddServiceDefaults();

builder.Services.AddEndpointsApiExplorer();

// From Aspire
builder.AddAzureOpenAIClient("openai");

builder.Services.Configure<VoiceRagOptions>(
    builder.Configuration.GetSection(VoiceRagOptions.SectionName));

builder.Services.AddSingleton<IAIToolHandler, ReferenceDataTools>();
builder.Services.AddSingleton<IAIToolRegistry, AIToolRegistry>();

var teamsAppId = builder.Configuration.GetValue<string>("TeamsAppId");
var teamsAppIdentifier = new MicrosoftTeamsAppIdentifier(teamsAppId);

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<VoiceRagOptions>>().Value;
    return new CallAutomationClient(connectionString: options.AcsConnectionString);
});

var app = builder.Build();


app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

var appBaseUrl = Environment.GetEnvironmentVariable("VS_TUNNEL_URL")?.TrimEnd('/');

if (string.IsNullOrEmpty(appBaseUrl))
{
    var websiteHostName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
    Console.WriteLine($"websiteHostName :{websiteHostName}");
    appBaseUrl = $"https://{websiteHostName}";
    Console.WriteLine($"appBaseUrl :{appBaseUrl}");
}

app.MapGet("/", () => "Hello ACS CallAutomation!");

app.MapPost("/api/incomingCall", async (
    [FromBody] EventGridEvent[] eventGridEvents,
    [FromServices] CallAutomationClient client,
    ILogger <Program> logger) =>
{
    foreach (var eventGridEvent in eventGridEvents)
    {
        Console.WriteLine($"Incoming Call event received.");

        // Handle system events
        if (eventGridEvent.TryGetSystemEventData(out object eventData))
        {
            // Handle the subscription validation event.
            if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
            {
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = subscriptionValidationEventData.ValidationCode
                };
                return Results.Ok(responseData);
            }
        }

        var jsonObject = Helper.GetJsonObject(eventGridEvent.Data);
        var callerId = Helper.GetCallerId(jsonObject);
        var incomingCallContext = Helper.GetIncomingCallContext(jsonObject);
        var callbackUri = new Uri(new Uri(appBaseUrl), $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
        logger.LogInformation($"Callback Url: {callbackUri}");
        var websocketUri = appBaseUrl.Replace("https", "wss") + "/ws";
        logger.LogInformation($"WebSocket Url: {callbackUri}");

        var mediaStreamingOptions = new MediaStreamingOptions(
                new Uri(websocketUri),
                MediaStreamingContent.Audio,
                MediaStreamingAudioChannel.Mixed,
                startMediaStreaming: true
                )
        {
            EnableBidirectional = true,
            AudioFormat = AudioFormat.Pcm24KMono
        };

        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
        {
            MediaStreamingOptions = mediaStreamingOptions,
        };

        AnswerCallResult answerCallResult = await client.AnswerCallAsync(options);
        logger.LogInformation($"Answered call for connection id: {answerCallResult.CallConnection.CallConnectionId}");
    }
    return Results.Ok();
});

// api to handle call back events
app.MapPost("/api/callbacks/{contextId}", (
    [FromBody] CloudEvent[] cloudEvents,
    [FromRoute] string contextId,
    [Required] string callerId,
    ILogger<Program> logger) =>
{

    foreach (var cloudEvent in cloudEvents)
    {
        CallAutomationEventBase @event = CallAutomationEventParser.Parse(cloudEvent);
        logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event, Formatting.Indented)}");
    }

    return Results.Ok();
});

app.UseWebSockets();

#pragma warning disable OPENAI002
app.MapGet("/ws", async (HttpContext context, IOptions<VoiceRagOptions> configurationOptions, AzureOpenAIClient openAIClient, IAIToolRegistry toolRegistry) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        try
        {
            var config = configurationOptions.Value;

            //var voiceClient = openAIClient.AsVoiceClient(config.AzureOpenAIDeploymentModelName, voiceClientLogger);
            var realtimeClient = openAIClient.GetRealtimeConversationClient(config.AzureOpenAIDeploymentModelName);

            //IList<AITool> tools = [AIFunctionFactory.Create(GetRoomCapacity)];

            RealtimeSessionOptions sessionOptions = new()
            {
                Instructions = config.AzureOpenAISystemPrompt,
                Voice = ConversationVoice.Shimmer,
                InputAudioFormat = ConversationAudioFormat.Pcm16,
                OutputAudioFormat = ConversationAudioFormat.Pcm16,
                Tools = toolRegistry.ToArray(),
                //InputTranscriptionOptions = new()
                //{
                //// OpenAI realtime excepts raw audio in/out and uses another model for transcriptions in parallel
                //// Currently, it only supports whisper v2 (named whisper-1) for transcription 
                //// Note, this means that the transcription will be done by a different model than the one generating the response, which may lead to differences between the audio and transcription
                //    Model = "whisper-1", 
                //},
                TurnDetectionOptions = ConversationTurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(0.5f, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500)),
            };
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var acsParticipant = new AcsCallParticipant(webSocket, name: "CallerName");
            var openAIParticipant = new OpenAIRealtimeAgent(realtimeClient, sessionOptions, name: "OpenAI");
            openAIParticipant.SubscribeTo(acsParticipant);
            acsParticipant.SubscribeTo(openAIParticipant);
           
            var task2 = acsParticipant.StartResponseAsync(context.RequestAborted);
            var task1 = openAIParticipant.StartResponseAsync(context.RequestAborted);
            await Task.WhenAll(task1, task2);
            //await voiceClient.StartConversationAsync(new AcsAIOutboundHandler(webSocket, logger: acsOutboundHandlerLogger), sessionOptions, cancellationToken: context.RequestAborted);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception received {ex}");
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.Run();

[Description("Returns the number of people that can fit in a room.")]
static int GetRoomCapacity(RoomType roomType)
{
    return roomType switch
    {
        RoomType.ShuttleSimulator => throw new InvalidOperationException("No longer available"),
        RoomType.NorthAtlantisLawn => 450,
        RoomType.VehicleAssemblyBuilding => 12000,
        _ => throw new NotSupportedException($"Unknown room type: {roomType}"),
    };
}

enum RoomType
{
    ShuttleSimulator,
    NorthAtlantisLawn,
    VehicleAssemblyBuilding,
}

