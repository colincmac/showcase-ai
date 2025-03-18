#pragma warning disable OPENAI002

using OpenAI.Chat;
using OpenAI.RealtimeConversation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public class ConversationHistory : IList<ConversationItem>, IReadOnlyList<ConversationItem>
{
    private readonly List<ConversationItem> _messages = [];

    public ConversationItem this[int index]
    {
        get => _messages[index];
        set => _messages[index] = value;
    }

    public int Count => _messages.Count;

    public bool IsReadOnly => false;

    public void Add(ConversationItem item)
    {
        _messages.Add(item);
    }
    public void AddRange(IEnumerable<ConversationItem> items)
    {
        _messages.AddRange(items);
    }

    public void Clear()
    {
        _messages.Clear();
    }

    public bool Contains(ConversationItem item)
    {
        return _messages.Contains(item);
    }

    public void CopyTo(ConversationItem[] array, int arrayIndex)
    {
        _messages.CopyTo(array, arrayIndex);
    }

    public IEnumerator<ConversationItem> GetEnumerator()
    {
        return _messages.GetEnumerator();
    }

    public int IndexOf(ConversationItem item)
    {
        return _messages.IndexOf(item);
    }

    public void Insert(int index, ConversationItem item)
    {
        _messages.Insert(index, item);
    }

    public bool Remove(ConversationItem item)
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
