#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class ConversationAudioAgent : IAgent, IDisposable
{

    private readonly ILogger<ConversationAudioAgent> _logger;
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private RealtimeConversationSession _session;
    private readonly Channel<AiAgentCommand> _commandChannel;

    public ConversationAudioAgent(ILogger<ConversationAudioAgent> logger, RealtimeConversationSession session)
    {
        _logger = logger;
        _session = session;
        _commandChannel = Channel.CreateUnbounded<AiAgentCommand>();
    }

    public async Task ProcessInboundDataAsync(DataFrame frame, CancellationToken cancellationToken)
    {
        try
        {
            // Todo, accept either audio or text.
            using MemoryStream ms = new (frame.Buffer);
            await _session.SendInputAudioAsync(ms, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending audio to OpenAI session.");
        }
    }

    public Task ProcessOutboundDataAsync(DataFrame frame, CancellationToken cancellationToken)
    {
        // Outbound data, audio in this case, is produced by the conversation agent and the agent doesn't need to review its own output.
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _commandChannel.Writer.Complete();
        _session.Dispose();
    }
}
