using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.RealtimeConversation;
using System.Buffers;
using System.Diagnostics.Tracing;
using System.Threading.Channels;


namespace Showcase.AudioOrchestration;


#pragma warning disable OPENAI002

public abstract class RealtimeAgent
{
    //private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;


    internal ILogger _logger;

    internal CancellationTokenSource _cts = new();
    internal Channel<RealtimeEvent> _outputChannel;
    internal Channel<RealtimeEvent> _inputChannel;

    private readonly Channel<RealtimeEvent> _updates = Channel.CreateUnbounded<RealtimeEvent>();

    private readonly RealtimeEventObservable _realtimeEventObservable = new();


    public string? Instructions { get; init; }
    public string? Description { get; init; }
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string? Name { get; init; }

    public RealtimeAgent(
        ILogger? logger = null)
    {
        _outputChannel = Channel.CreateUnbounded<RealtimeEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _inputChannel = Channel.CreateUnbounded<RealtimeEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _logger = logger ?? NullLogger.Instance;

    }

    public IObservable<RealtimeEvent> Watch() => _realtimeEventObservable;


    private void BeginProcessEntityUpdates() =>
    Task<Task?>.Factory.StartNew(
        ProcessEntityUpdates,
        _cts.Token,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default);

    private async Task ProcessEntityUpdates()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested ||
                !_updates.Reader.Completion.IsCompleted)
            {
                var update = await _updates.Reader.ReadAsync(_cts.Token);
                _entityUpdateObservable.OnUpdated(update);
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
            _updates.Writer.TryComplete();
            _entityUpdateObservable.OnComplete();
        }
    }

    public virtual async ValueTask SendAsync(
        RealtimeEvent incomingEvent,
        CancellationToken cancellationToken = default)
    {
        await _inputChannel.Writer.WriteAsync(incomingEvent, cancellationToken);
    }

    public abstract IAsyncEnumerable<RealtimeEvent> StartResponseAsync(CancellationToken cancellationToken = default);

}
