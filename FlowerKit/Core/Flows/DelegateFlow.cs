using System;

namespace FlowerKit.Core.Flows;

/// <summary>
/// A flow that runs a simple Action.
/// </summary>
public class DelegateFlow<T>(Action<FlowContext<T>> action) : Flow
    where T : Event
{
    public override Type EventType => typeof(T);

    public override void Run(object ev)
        => action(
            new FlowContext<T> {
                Data = (T)ev
            }
        );
    public override void Plan()
        => Planner.Current.AddToPlan(this, typeof(T));
}