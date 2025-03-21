#pragma warning disable OPENAI002

using Microsoft.Extensions.AI;
using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public abstract class RealtimeAgentChannel
{
    private readonly ConversationHistory _history;


    protected internal abstract Task ReceiveAsync(IEnumerable<RealtimeEvent> history, CancellationToken cancellationToken = default);

    protected internal abstract Task ResetAsync(CancellationToken cancellationToken = default);

    protected internal abstract IAsyncEnumerable<RealtimeEvent> InvokeAsync(
    RealtimeAgent agent,
    CancellationToken cancellationToken = default);

    protected internal abstract IAsyncEnumerable<RealtimeEvent> GetHistoryAsync(CancellationToken cancellationToken = default);

}
