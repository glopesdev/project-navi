using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bonsai.Expressions;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace ProjectNavi.Bonsai.Kinect
{
    public class ThreadPool : CombinatorBuilder
    {
        protected override IObservable<TSource> Combine<TSource>(IObservable<TSource> source)
        {
            return source.ObserveOn(Scheduler.ThreadPool);
        }
    }
}
