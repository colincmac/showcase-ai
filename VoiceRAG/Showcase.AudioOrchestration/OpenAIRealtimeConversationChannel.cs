#pragma warning disable OPENAI002

using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants.GenAI;

namespace Showcase.AudioOrchestration;

public class OpenAIRealtimeConversationChannel : IRealtimeConversationChannel
{
    private readonly string conversationId;
    private readonly RealtimeConversationClient client;
    private readonly ConversationSessionOptions options;
    private readonly ConversationHistory _conversationHistory = [];

    public OpenAIRealtimeConversationChannel(
    string conversationId,
    RealtimeConversationClient client,
    RealtimeConversationSession session,
    ConversationSessionOptions options)
    {
        this.conversationId = conversationId;
        this.client = client;
        this.options = options;
    }

    public Task ReceiveAsync(IEnumerable<ConversationItem> history, CancellationToken cancellationToken)
    {
        _conversationHistory.AddRange(history);
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<ConversationUpdate> StartConversationAsync(
    RealtimeAgent session,
    CancellationToken cancellationToken = default)
    {

    }

    private async Task<ConversationUpdate> HandleConversationUpdate() 
    {

    }

}
