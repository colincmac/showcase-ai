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
    public ChannelReader<T> Subscribe(int capacity = 1000)
    {
        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false
        });
        lock (_lock)
        {
            _channels.Add(channel);
        }
        return channel.Reader;
    }

    // Publish the data to all subscribers.
    // NOTE: This method awaits each subscriber's callback so that the data remains valid.
    public async Task PublishAsync(T message)
    {
        List<Channel<T>> channelsSnapshot;
        lock (_lock)
        {
            channelsSnapshot = [.. _channels];
        }
        foreach (var channel in channelsSnapshot)
        {
            // For ultra-low latency, you might choose TryWrite with a fallback if a channel is slow.
            await channel.Writer.WriteAsync(message);
        }
    }
}
