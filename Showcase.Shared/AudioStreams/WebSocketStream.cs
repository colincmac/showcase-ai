using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Showcase.Shared.AudioStreams;

public class WebSocketStream : Stream, IDisposable
{

    // Used for reconnect (when enabled) to determine if the close was ungraceful or not, reconnect only happens on ungraceful disconnect
    // The assumption is that a graceful close was triggered purposefully by either the client or server and a reconnect shouldn't occur 
    private bool _gracefulClose;

    private readonly WebSocket _streamWebSocket;
    private readonly object _lock = new();
    private bool IsDisposed { get; set; }

    // Whether the stream should dispose of the socket when the stream is disposed
    private readonly bool _ownsSocket;


    public WebSocketStream(WebSocket webSocket, bool ownsSocket =false)
    {
        _streamWebSocket = webSocket;
        _ownsSocket = ownsSocket;
    }

    public WebSocket WebSocket => _streamWebSocket;

    public bool IsClosed => _streamWebSocket.State != WebSocketState.Open;

    // Indicates that data can be read from the stream.
    // We return the readability of this stream.
    public override bool CanRead => !IsDisposed;

    // Indicates that the stream can seek a specific location
    // in the stream. This property always returns false.
    public override bool CanSeek => false;

    public override bool CanWrite => !IsDisposed;

    public override long Length => throw new NotSupportedException();

    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }


    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        // No-op for WebSocket since they do not need to be flushed.
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return _streamWebSocket.SendAsync(buffer, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var result = await _streamWebSocket.ReceiveAsync(buffer, cancellationToken);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            return 0;
        }

        return result.Count;
    }

    protected override void Dispose(bool disposing)
    {
        IsDisposed = true;
        _streamWebSocket.Dispose();
        base.Dispose(disposing);
    }
}
