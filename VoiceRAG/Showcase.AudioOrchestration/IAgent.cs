using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public interface IAgent
{
    string Name { get; }

    Task ReceiveAsync(AudioFrame audioFrame, CancellationToken ct);

    // Handle a message from another agent (internal backchannel)
    Task OnAgentMessageAsync(BinaryData message, CancellationToken ct);
}
