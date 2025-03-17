//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.IO.Pipelines;
//using System.Linq;
//using System.Text;
//using System.Threading.Channels;
//using System.Threading.Tasks;
//using static Showcase.Shared.AIExtensions.Realtime.Telemetry.OpenTelemetryConstants;

//namespace Showcase.AudioOrchestration;

//public class ConversationParticipant
//{
//    // Channels for receiving broadcasts.
//    private ChannelReader<byte[]>? _audioReader;
//    private ChannelReader<BinaryData>? _aiEventReader;

//    // Method for a participant to subscribe to the broadcast channels.
//    public void SubscribeAudio(ChannelReader<byte[]> reader)
//    {
//        _audioReader = reader;
//        _ = Task.Run(() => ListenAudioAsync());
//    }

//    public void SubscribeAiEvents(ChannelReader<BinaryData> reader)
//    {
//        _aiEventReader = reader;
//        _ = Task.Run(() => ListenAiEventsAsync());
//    }

//    public virtual async Task ListenAudioAsync()
//    {
//        if (_audioReader == null) throw new InvalidOperationException("Audio reader is not initialized.");
        
//        await foreach (var audioData in _audioReader.ReadAllAsync())
//        {
//            // Process the received audio data from others.
//        }
//    }

//    public virtual async Task ListenAiEventsAsync()
//    {
//        if (_aiEventReader == null) throw new InvalidOperationException("AI event reader is not initialized.");

//        await foreach (var aiEvent in _aiEventReader.ReadAllAsync())
//        {
//            // Process the received AI event from others.
//        }
//    }

//    // Publishing: reading from a local PipeReader and broadcasting.
//    private async Task ProcessAudioStreamAsync(PipeReader reader, Broadcaster<byte[]> broadcaster, CancellationToken cancellationToken)
//    {
//        while (true)
//        {
//            var result = await reader.ReadAsync(cancellationToken);
//            ReadOnlySequence<byte> buffer = result.Buffer;

//            if (!buffer.IsEmpty)
//            {
//                // Process and/or clone data as needed. For PCM audio, you might 
//                // consider using MemoryPool to avoid excessive allocations.
//                byte[] audioData = buffer.ToArray();
//                // Broadcast the data to all listeners.
//                await broadcaster.PublishAsync(audioData);
//            }

//            reader.AdvanceTo(buffer.End);
//            if (result.IsCompleted)
//            {
//                break;
//            }
//        }

//        await reader.CompleteAsync();
//    }
//}
