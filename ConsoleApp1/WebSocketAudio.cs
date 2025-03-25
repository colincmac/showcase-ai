using System.Threading.Tasks;
using System.Threading;
using System;
using System.Net.WebSockets;    
using NAudio.Wave;
public class WebSocketAudio : IDisposable
{
    private readonly WebSocket _webSocket;
    private readonly BufferedWaveProvider _waveProvider;
    private readonly WaveInEvent _waveInEvent;
    private readonly WaveOutEvent _waveOutEvent;

    public WebSocketAudio(string uri)
    {
        _webSocket = new WebSocket();
        _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None).Wait();

        WaveFormat audioFormat = new WaveFormat(24000, 16, 1);
        _waveProvider = new BufferedWaveProvider(audioFormat)
        {
            BufferDuration = TimeSpan.FromMinutes(2),
        };

        _waveInEvent = new WaveInEvent
        {
            WaveFormat = audioFormat,
            BufferMilliseconds = 50
        };
        _waveInEvent.DataAvailable += OnDataAvailable;

        _waveOutEvent = new WaveOutEvent();
        _waveOutEvent.Init(_waveProvider);
        _waveOutEvent.Play();
    }

    private async void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.SendAsync(new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    public async Task ReceiveAudioAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                _waveProvider.AddSamples(buffer, 0, result.Count);
            }
        }
    }

    public void StartRecording()
    {
        _waveInEvent.StartRecording();
    }

    public void StopRecording()
    {
        _waveInEvent.StopRecording();
    }

    public void Dispose()
    {
        _waveInEvent?.Dispose();
        _waveOutEvent?.Dispose();
        _webSocket?.Dispose();
    }
}
}