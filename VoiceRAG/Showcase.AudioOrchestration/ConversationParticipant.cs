using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Showcase.AudioOrchestration;
using System.Threading.Channels;

public abstract class ConversationParticipant : IDisposable
{
    protected ILogger _logger;
    protected CancellationTokenSource _cts = new();
    protected Channel<RealtimeEvent> _inboundChannel;
    protected Channel<RealtimeEvent> _outboundChannel = Channel.CreateUnbounded<RealtimeEvent>();
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
        _logger = logger ?? NullLogger.Instance;
        StartBroadcastingOutbound(_cts.Token);
    }

    public IObservable<RealtimeEvent> Watch() => _outgoingEventsObservable;
    public void SubscribeTo(ConversationParticipant conversationParticipant) => conversationParticipant.Watch().Subscribe(async evt => await SendAsync(evt, _cts.Token));

    public virtual void Send(RealtimeEvent incomingEvent)
    {
        _inboundChannel.Writer.TryWrite(incomingEvent);
    }

    public virtual async Task SendAsync(RealtimeEvent incomingEvent, CancellationToken cancellationToken)
    { 
         await  _inboundChannel.Writer.WriteAsync(incomingEvent, cancellationToken);
    }

    public abstract Task StartResponseAsync(CancellationToken cancellationToken = default);


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