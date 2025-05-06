using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI.RealtimeConversation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.ConversationParticipants.CallSafetyAuditor;
public class ChatAuditorParticipant : ConversationParticipant
{

    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;
    internal Task ParticipantEventProcessing { get; private set; } = Task.CompletedTask;
    private readonly IChatClient _contentSafetyChatClient;
    protected Channel<string> _partialTranscriptBuffer;

    public ChatAuditorParticipant(IChatClient contentSafetyChatClient, string? id, string? name, ILogger? logger = null) : base(id, name, logger)
    {
        // Currently, we need to use a streaming annotation to get the content safety results. This is enabled with async filters.
        _contentSafetyChatClient = contentSafetyChatClient ?? throw new ArgumentNullException(nameof(contentSafetyChatClient));
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        InternalEventProcessing = Task.Run(() => ProcessInboundEvents(_cts.Token), _cts.Token);
        await Task.WhenAll(InternalEventProcessing).ConfigureAwait(false);
    }
   

    private async Task ProcessInboundEvents(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var internalEvent in _inboundChannel.Reader.ReadAllAsync(cancellationToken))
            {

                if (internalEvent is RealtimeTranscriptFinishedEvent transcriptEvent && !transcriptEvent.IsEmpty)
                    await HandleTranscriptAsync(transcriptEvent, cancellationToken);

                if (internalEvent is RealtimeAudioDeltaEvent audioEvent && !audioEvent.IsTranscriptEmpty)
                    await HandleTranscriptDeltaAsync(audioEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbound events: {Message}", ex.Message);
        }
    }

    private async Task HandleTranscriptAsync(RealtimeTranscriptFinishedEvent transcriptEvent, CancellationToken cancellationToken)
    {
        
        await foreach (var response in _contentSafetyChatClient.GetStreamingResponseAsync(
            chatMessage: new ChatMessage(role: new ChatRole(transcriptEvent.ConversationRole), 
            content: transcriptEvent.Transcription),
            cancellationToken: cancellationToken))
        {
           _logger.LogDebug("Received response: {Response}", response);
        }
    }
    private Task HandleTranscriptDeltaAsync(RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        // Currently not implemented, but we could use this to send partial transcripts to the content safety client.
        return Task.CompletedTask;
    }
}
