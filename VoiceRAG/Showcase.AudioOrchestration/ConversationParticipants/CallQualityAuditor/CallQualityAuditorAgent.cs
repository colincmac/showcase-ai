using Microsoft.Extensions.Logging;
using Showcase.AI.Voice.ConversationParticipants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice.ConversationParticipants.CallQualityAuditor;

public class CallQualityAuditorAgent : ConversationParticipant
{
    internal Task InternalEventProcessing { get; private set; } = Task.CompletedTask;

    public CallQualityAuditorAgent(string? id, string? name, ILogger? logger = null) : base(id, name, logger)
    {
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
                if (internalEvent is RealtimeAudioDeltaEvent audioEvent)
                    await HandleAudioAsync(audioEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing inbound events: {Message}", ex.Message);
        }
    }

    private Task HandleAudioAsync(RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

}
