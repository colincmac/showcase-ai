#pragma warning disable OPENAI002

using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public interface IRealtimeConversationChannel
{
    Task ReceiveAsync(IEnumerable<ConversationItem> history, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ConversationUpdate> StartConversationAsync(
    RealtimeAgent agent,
    CancellationToken cancellationToken = default);
}
