using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;


/// <summary>
/// holds a list of subscriber channels. When data is published, it’s written to every subscribed participant’s channel. 
/// The design of the broadcaster minimizes locking (only for subscription changes) and uses asynchronous writes to keep latency low.
/// </summary>
public class Broadcaster<T>
{
    private readonly List<Channel<T>> _channels = new();
    private readonly Lock _lock = new();

    // Subscribers get their own channel.
    public ChannelReader<T> Subscribe(int capacity = 5000)
    {
        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity: capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait // backpressure
        });

        using (_lock.EnterScope())
        {
            _channels.Add(channel);
        }
        return channel.Reader;
    }

    public async Task PublishAsync(T message)
    {
        List<Channel<T>> channelsSnapshot;
        using (_lock.EnterScope())
        {
            // Take a snapshot to avoid holding the lock during I/O.
            channelsSnapshot = [.. _channels];
        }
        // Publish the message to each subscriber.
        foreach (var channel in channelsSnapshot)
        {
            // You might consider a TryWrite and drop strategy if a subscriber is slow.
            await channel.Writer.WriteAsync(message);
        }
    }

    private async Task ProcessAudioStreamAsync(PipeReader reader, Broadcaster<byte[]> broadcaster, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = await reader.ReadAsync(cancellationToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (!buffer.IsEmpty)
            {
                // Process and/or clone data as needed. For PCM audio, you might 
                // consider using MemoryPool to avoid excessive allocations.
                byte[] audioData = buffer.ToArray();
                // Broadcast the data to all listeners.
                await broadcaster.PublishAsync(audioData);
            }

            reader.AdvanceTo(buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync();
    }

}
