using Showcase.AudioOrchestration;
using System;
using System.Buffers;
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

            if (internalEvent is ParticipantSpeakingEvent stopAudioEvent) await SendStopAudioAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ProcessParticipantEventsAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 16);
        MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.Start();

        while (true)
        {
            int bytesRead = await microphoneInput.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }
            ReadOnlyMemory<byte> audioMemory = buffer.AsMemory(0, bytesRead);
            BinaryData audioData = BinaryData.FromBytes(audioMemory);

            var audioEvent = new RealtimeAudioDeltaEvent(AudioData: audioData); // Replace with actual audio data
            await _outboundChannel.Writer.WriteAsync(audioEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task SendAudioAsync(RealtimeAudioDeltaEvent audioEvent, CancellationToken cancellationToken)
    {
        // Process the audio event
        if(audioEvent.IsEmpty) return Task.CompletedTask;
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
            var outbound = Task.Run(() => ProcessParticipantEventsAsync(cancellationToken), cancellationToken);
        await Task.WhenAll(inbound, outbound).ConfigureAwait(false);
    }
}
