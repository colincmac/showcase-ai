using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.Shared.AudioStreams;

public class AudioStreamDataPooledBuffer
{
    private readonly object _lock = new();
    private readonly AudioStreamData[] _buffer;
    private int _head;
    private int _tail;
    private int _allocated;
    public string CorrelationId { get; set; } = string.Empty;

    public int Count { get; private set; }
    public bool IsFull => Count == _buffer.Length;
    public bool IsEmpty => Count == 0;

    public AudioStreamDataPooledBuffer(AudioStreamData[] buffer)
    {
        _buffer = buffer;

        _head = 0;
        _tail = 0;
        _allocated = 0;
        Count = 0;
    }
}
