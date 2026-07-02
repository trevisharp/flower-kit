using System;

namespace FlowerKit.Flows;

/// <summary>
/// A flow that runs a simple Action.
/// </summary>
public class DelegateFlow(Action<FlowContext> action) : Flow
{
    public override void Run(FlowContext ctx)
        => action(ctx);
}