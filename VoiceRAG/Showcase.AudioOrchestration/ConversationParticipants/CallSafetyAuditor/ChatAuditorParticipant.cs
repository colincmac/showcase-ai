using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.ConversationParticipants.CallSafetyAuditor;
public class ChatAuditorParticipant : ConversationParticipant
{

    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;
    private readonly IChatClient _contentSafetyChatClient;
    protected Channel<string> _partialTranscriptBuffer;
    private const int ResponseTranscriptSegments = 5;
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
                if (internalEvent is RealtimeAudioDeltaEvent audioEvent && !audioEvent.IsTranscriptEmpty)
                    await HandleTranscriptAsync(audioEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbound events: {Message}", ex.Message);
        }
    }

    private async Task HandleTranscriptAsync(RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        var role = Enum.TryParse<ChatRole>(audioEvent.ConversationRole, true, out var parsedRole) ? parsedRole : ChatRole.Assistant;
        await foreach(var response in _contentSafetyChatClient.GetStreamingResponseAsync(
            chatMessage: new ChatMessage(role: role, content: audioEvent.TranscriptText),
            cancellationToken: cancellationToken))
        {
            _logger.LogDebug("Received response: {Response}", response);

        };
    }
}
