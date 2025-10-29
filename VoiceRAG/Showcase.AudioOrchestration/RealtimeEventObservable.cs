using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Showcase.AI.Voice;

public sealed class RealtimeEventObservable : IObservable<RealtimeEvent>
{
    private readonly Lock _sync = new();
    private ImmutableList<IObserver<RealtimeEvent>> _observers =
        ImmutableList<IObserver<RealtimeEvent>>.Empty;


    public IDisposable Subscribe([NotNull] IObserver<RealtimeEvent> observer)
    {

        lock (_sync)
        {
            _observers = _observers.Add(observer);
        }

        return new Subscription(this, observer);
    }

    public void OnUpdated(RealtimeEvent update)
    {
        var observers = _observers;

        if (observers.Count > 0)
        {
            foreach (var observer in observers)
            {
                observer.OnNext(update);
            }
        }
    }

    public void OnComplete()
    {
        var observers = _observers;

        if (observers.Count > 0)
        {
            foreach (var observer in observers)
            {
                observer.OnCompleted();
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly RealtimeEventObservable _observable;
        private readonly IObserver<RealtimeEvent> _observer;
        private bool _disposed;

        public Subscription(
            RealtimeEventObservable observable,
            IObserver<RealtimeEvent> observer)
        {
            _observable = observable;
            _observer = observer;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_observable._sync)
                {
                    _observable._observers = _observable._observers.Remove(_observer);
                }
                _disposed = true;
            }
        }
    }
}
