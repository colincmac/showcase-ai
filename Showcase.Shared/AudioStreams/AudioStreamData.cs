using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.Shared.AudioStreams;

public class AudioStreamData
{
    public byte[] AudioData { get; private set; }
    public string? StreamId { get; set; }
    public string? SourceId { get; set; }
    public string? CorrelationId { get; set; }
    public int SampleRate { get; set; }
    public DateTime Timestamp { get; set; }


    public AudioStreamData(byte[] audioData)
    {
        AudioData = audioData;
    }
}
