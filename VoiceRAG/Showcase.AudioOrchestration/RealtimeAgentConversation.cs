#pragma warning disable OPENAI002

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using Showcase.Shared.AIExtensions.Realtime;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AI.Voice;

public class RealtimeAgentConversation
{
    internal ILogger _logger;
    internal List<ConversationParticipant> _agents = new();

    public RealtimeAgentConversation(WebSocket webSocket, RealtimeConversationClient realtimeAIClient, RealtimeSessionOptions sessionOptions, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RealtimeAgentConversation>();
        var acsParticipant = new AcsCallParticipant(webSocket, name: "CallerName", id: "ACS ID", loggerFactory: loggerFactory);
        var openAIParticipant = new OpenAIRealtimeAgent(realtimeAIClient, sessionOptions, name: "OpenAI", id: "ACS ID", loggerFactory: loggerFactory);
        openAIParticipant.SubscribeTo(acsParticipant);
        acsParticipant.SubscribeTo(openAIParticipant);
        _agents.Add(acsParticipant);
        _agents.Add(openAIParticipant);

    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(_agents.Select(a => a.StartResponseAsync(cancellationToken)));
    }
}
