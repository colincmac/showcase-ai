using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Showcase.Shared.AudioStreams;

public class AudioStreamConnection
{

    private const int _defaultObjectPoolSize = 1000;
    private readonly ILogger<AudioStreamConnection> _logger;
    private readonly object _mainLock = new ();

    public DateTime? StreamStartTime { get; set; }
    public string CorrelationId { get; private set; } = string.Empty;
    private bool _streamIsComplete;
    private AudioStreamData _streamData;
    private AudioStreamDataPooledBuffer _buffer;
    private uint _lastAudioStreamDataByteSize;


    private void Initialize(uint audioStreamDataByteSize, int objectPoolSize = _defaultObjectPoolSize)
    {
        _lastAudioStreamDataByteSize = audioStreamDataByteSize;
        var data = new AudioStreamData[objectPoolSize];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = new AudioStreamData(new byte[audioStreamDataByteSize]);
        }

        _buffer = new AudioStreamDataPooledBuffer(data)
        {
            CorrelationId = CorrelationId
        };
    }

    //public AudioStreamConnection(ILogger<AudioStreamConnection> logger, uint audioStreamDataByteSize, int objectPoolSize = _defaultObjectPoolSize)
    //{
    //    _logger = logger;
    //    Initialize(audioStreamDataByteSize, objectPoolSize);
    //}


    //public AudioStreamData DecodeBuffer(byte[] buffer, int count)
    //{
    //    lock (_mainLock)
    //    {
    //        if (_streamIsComplete)
    //        {
    //            throw new InvalidOperationException("Stream is complete");
    //        }
    //        if (buffer.Length != _lastAudioStreamDataByteSize)
    //        {
    //            throw new ArgumentException($"Buffer length {buffer.Length} does not match expected length {_lastAudioStreamDataByteSize}");
    //        }
    //        var stringContent = Encoding.UTF8.GetString(buffer, 0, count);

    //        var audio = JsonSerializer.Deserialize<AudioStreamData>(stringContent);

    //        var audioStreamData = _buffer.Get();
    //        audioStreamData.AudioData = buffer;
    //        audioStreamData.Timestamp = DateTime.UtcNow;
    //        audioStreamData.CorrelationId = CorrelationId;
    //        return audioStreamData;
    //    }
    //}
}
