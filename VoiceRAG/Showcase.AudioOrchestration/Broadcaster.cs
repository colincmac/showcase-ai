//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.IO.Pipelines;
//using System.Linq;
//using System.Text;
//using System.Threading.Channels;
//using System.Threading.Tasks;

//namespace Showcase.AudioOrchestration;


///// <summary>
///// holds a list of subscriber channels. When data is published, it’s written to every subscribed participant’s channel. 
///// The design of the broadcaster minimizes locking (only for subscription changes) and uses asynchronous writes to keep latency low.
///// </summary>
//public class Broadcaster<T>
//{
//    private readonly List<Channel<T>> _channels = [];
//    private readonly Lock _lock = new();

//    private readonly int _channelCapacity;

//    public Broadcaster(int channelCapacity = 5000)
//    {
//        _channelCapacity = channelCapacity;
//    }

//    // Subscribers get their own channel.
//    public ChannelReader<T> Subscribe()
//    {
//        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity: _channelCapacity)
//        {
//            SingleReader = true,
//            SingleWriter = false,
//            FullMode = BoundedChannelFullMode.Wait // backpressure
//        });

//        using (_lock.EnterScope())
//        {
//            _channels.Add(channel);
//        }
//        return channel.Reader;
//    }

//    public async Task PublishAsync(T message)
//    {
//        List<Channel<T>> channelsSnapshot;
//        using (_lock.EnterScope())
//        {
//            // Take a snapshot to avoid holding the lock during I/O.
//            channelsSnapshot = [.. _channels];
//        }
//        // Publish the message to each subscriber.
//        foreach (var channel in channelsSnapshot)
//        {
//            // You might consider a TryWrite and drop strategy if a subscriber is slow.
//            await channel.Writer.WriteAsync(message);
//        }
//    }
//}
