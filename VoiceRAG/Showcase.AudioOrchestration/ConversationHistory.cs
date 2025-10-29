#pragma warning disable OPENAI002

using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice;

public class ConversationHistory : IList<RealtimeEvent>, IReadOnlyList<RealtimeEvent>
{
    private readonly List<RealtimeEvent> _messages = [];

    public RealtimeEvent this[int index]
    {
        get => _messages[index];
        set => _messages[index] = value;
    }

    public int Count => _messages.Count;

    public bool IsReadOnly => false;

    public void Add(RealtimeEvent item)
    {
        _messages.Add(item);
    }
    public void AddRange(IEnumerable<RealtimeEvent> items)
    {
        _messages.AddRange(items);
    }

    public void Clear()
    {
        _messages.Clear();
    }

    public bool Contains(RealtimeEvent item)
    {
        return _messages.Contains(item);
    }

    public void CopyTo(RealtimeEvent[] array, int arrayIndex)
    {
        _messages.CopyTo(array, arrayIndex);
    }

    public IEnumerator<RealtimeEvent> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }

    public int IndexOf(RealtimeEvent item)
    {
        return _messages.IndexOf(item);
    }

    public void Insert(int index, RealtimeEvent item)
    {
        _messages.Insert(index, item);
    }

    public bool Remove(RealtimeEvent item)
    {
        return _messages.Remove(item);
    }

    public void RemoveAt(int index)
    {
        _messages.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
