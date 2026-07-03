using System;

namespace FlowerKit.Flows;

/// <summary>
/// A flow that runs a simple Action.
/// </summary>
public class DelegateFlow<T>(Action<FlowContext<T>> action) : Flow
    where T : Event
{
    public override void Run(object ctx)
        => action(
            ctx as FlowContext<T> ?? 
            throw new Exception("Context is null.")
        );
}