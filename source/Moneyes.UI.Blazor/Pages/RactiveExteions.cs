using Microsoft.AspNetCore.Components;
using Reactives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.Blazor.Pages
{
    class ReactiveComponentBase : ComponentBase
    {
        internal ISubject<bool> Disposed { get; } = new Subject<bool>();
    }

    //internal static class RactiveExteions
    //{
    //    public static IRef<TRet> ToProperty<TObj, TRet>(this TObj target, IObservable<TRet?> observable, TRet initialValue, Action<TRet> setter)
    //        where TObj : ReactiveComponentBase
    //    {
    //        var scheduler = CurrentThreadScheduler.Instance;
    //        return observable
    //            .TakeUntil(target.Disposed)
    //            .ObserveOn(scheduler)
    //            .
    //    }
    //}


}
