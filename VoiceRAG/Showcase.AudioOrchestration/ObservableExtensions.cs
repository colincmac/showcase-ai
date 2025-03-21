using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Showcase.AudioOrchestration;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(
    this IObservable<T> observable,
    Action<T> next,
    Action? complete = null) =>
    observable.Subscribe(new DelegateObserver<T>(next, complete));
}
