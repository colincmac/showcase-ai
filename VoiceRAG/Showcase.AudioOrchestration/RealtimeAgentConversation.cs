#pragma warning disable OPENAI002

using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using Showcase.AI.Voice.ConversationParticipants;
using Showcase.Shared.AIExtensions.Realtime;

using System.Net.WebSockets;


namespace Showcase.AI.Voice;

public class RealtimeAgentConversation
{
    internal ILogger _logger;
    internal List<ConversationParticipant> _agents = [];

    public RealtimeAgentConversation(WebSocket webSocket, RealtimeConversationClient realtimeAIClient, RealtimeSessionOptions sessionOptions, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RealtimeAgentConversation>();
        var acsParticipant = new AcsCallParticipant(webSocket, name: "CallerName", id: "ACS ID", loggerFactory: loggerFactory);
        var openAIParticipant = new OpenAIVoiceParticipant(realtimeAIClient, sessionOptions, name: "OpenAI", id: "ACS ID", loggerFactory: loggerFactory);
        openAIParticipant.SubscribeTo(acsParticipant);
        acsParticipant.SubscribeTo(openAIParticipant);
        _agents.Add(acsParticipant);
        _agents.Add(openAIParticipant);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_agents.Select(a => a.StartAsync(cancellationToken)));
    }
}
