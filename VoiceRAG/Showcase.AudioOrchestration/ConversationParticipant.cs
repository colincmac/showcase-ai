using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Showcase.AI.Voice;

/// <summary>
/// We have a few frameworks supporting Agents, Actors, and Processes.
/// https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/Experimental/Process.Runtime.Dapr/Actors/ProcessActor.cs
/// https://github.com/microsoft/autogen
/// https://github.com/microsoft/agent-runtime 
/// https://github.com/microsoft/semantic-kernel/issues/10418
/// There's also many general process frameworks like Orleans.
/// 
/// Many don't have a concept of a realtime "conversation" or "session" yet.
/// We need to identify where the perf matters. For example in Dapr, there's perf considerations for Actor activation and transport (HTTP)
/// In Daprs Actor model, we have a step actor and process actor.
/// 
/// Dapr Perf
/// https://docs.dapr.io/operations/performance-and-scalability/perf-actors-activation/
/// 
/// Considerations:
/// - Likely don't want to add unnecessary steps between actors/participants. Dapr adds a sidecar proxy.
/// - In Dapr and Orleans, actors are virtual and their lifetime is not tied to their in-memory representation
/// - This is likely better implemented as a service that scales based on calls or actors. Each pod is either a call or actor
/// </summary>
public abstract class ConversationParticipant : IDisposable
{
    protected ILogger _logger;
    protected CancellationTokenSource _cts = new();
    protected Channel<RealtimeEvent> _inboundChannel;
    protected Channel<RealtimeEvent> _outboundChannel;
    private readonly RealtimeEventObservable _outgoingEventsObservable = new();
    private bool _disposed;

    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string? Name { get; init; }

    public ConversationParticipant(
        string? id,
        string? name,
        ILogger? logger = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Name = name;

        _inboundChannel = Channel.CreateUnbounded<RealtimeEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        _outboundChannel = Channel.CreateUnbounded<RealtimeEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
        });

        _logger = logger ?? NullLogger.Instance;
        StartBroadcastingOutbound(_cts.Token);
    }

    #region Observable/reactive pattern
    // TODO: Review
    // - Should this support synchronous events?
    // - Should this be a separate class?
    // - Should this allow unsubscribing
    // - How is the mutable cancellation token handled?
    public IObservable<RealtimeEvent> Watch() => _outgoingEventsObservable;
    public void SubscribeTo(ConversationParticipant conversationParticipant) => conversationParticipant.Watch().Subscribe(async evt => await SendAsync(evt, _cts.Token));

    // TODO: Review, do we need to support non-async methods?
    //public virtual void Send(RealtimeEvent incomingEvent)
    //{
    //    _inboundChannel.Writer.TryWrite(incomingEvent);
    //}

    public virtual async Task SendAsync(RealtimeEvent incomingEvent, CancellationToken cancellationToken)
    { 
         await _inboundChannel.Writer.WriteAsync(incomingEvent, cancellationToken);
    }
    #endregion


    public abstract Task StartAsync(CancellationToken cancellationToken = default);

    // Review: Should we broadcast on StartAsync? Currently starting to broadcast immediately and leaving it up to the subscribers to process incomming events.
    private Task StartBroadcastingOutbound(CancellationToken cancellationToken) =>
        Task<Task?>.Factory.StartNew(
            () => BroadcastUpdatesAsync(cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

    private async Task BroadcastUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested ||
                !_outboundChannel.Reader.Completion.IsCompleted)
            {
                var update = await _outboundChannel.Reader.ReadAsync(cancellationToken);
                _outgoingEventsObservable.OnUpdated(update);
            }
        }
        catch (ObjectDisposedException)
        {
            // we ignore disposed exceptions.
        }
        catch (OperationCanceledException)
        {
            // we ignore cancellation exceptions.
        }
        catch (ChannelClosedException)
        {
            // we ignore cancellation exceptions.
        }
        finally
        {
            // we complete the update queue and also send a complete signal to our observers.
            _outboundChannel.Writer.TryComplete();
            _outgoingEventsObservable.OnComplete();
        }
    }

    public virtual void Dispose()
    {
        if (!_disposed)
        {
            _inboundChannel.Writer.TryComplete();
            _outboundChannel.Writer.TryComplete();
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}