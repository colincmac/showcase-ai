using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AI.Voice;

internal class DelegateObserver<T> : IObserver<T>
{
    private readonly Action<T> _next;
    private readonly Action? _complete;

    public DelegateObserver(Action<T> next, Action? complete = null)
    {
        _next = next;
        _complete = complete;
    }

    public void OnNext(T value) => _next(value);

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
        _complete?.Invoke();
    }
}
