using Showcase.AudioOrchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1;
public class TestParticipant : ConversationParticipant
{
    SpeakerOutput speakerOutput = new();

    public TestParticipant() : base("local", "local")
    {
    }

    public async Task ProcessInboundEventsAsync(CancellationToken cancellationToken)
    {
        await foreach (var internalEvent in _inboundChannel.Reader.ReadAllAsync(cancellationToken))
        {
            if (internalEvent is RealtimeAudioDeltaEvent audioEvent) await SendAudioAsync(audioEvent, cancellationToken).ConfigureAwait(false);

            if (internalEvent is RealtimeStopAudioEvent stopAudioEvent) await SendStopAudioAsync(cancellationToken).ConfigureAwait(false);
        }
    }
    public Task SendAudioAsync(RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        // Process the audio event
        
        speakerOutput.EnqueueForPlayback(audioEvent.AudioData);
        return Task.CompletedTask;
    }
    public Task SendStopAudioAsync(CancellationToken cancellationToken)
    {
        // Process the stop audio event
        speakerOutput.ClearPlayback();
        return Task.CompletedTask;
    }
    public override async Task StartResponseAsync(CancellationToken cancellationToken = default)
    {
            var inbound = Task.Run(() => ProcessInboundEventsAsync(cancellationToken), cancellationToken);
            var outboud = Task.Run(async () =>
            {
                using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.Start();
                var data = await BinaryData.FromStreamAsync(microphoneInput, cancellationToken).ConfigureAwait(false);
                var audioEvent = new RealtimeAudioDeltaEvent(data);
                await _outboundChannel.Writer.WriteAsync(audioEvent, cancellationToken).ConfigureAwait(false);
            });
        await Task.WhenAll(inbound, outboud).ConfigureAwait(false);
    }
}
