using System;

namespace FlowerKit;

using Core.StateExpressions;

/// <summary>
/// Represents a aggregation of events on a state.
/// </summary>
public record State(Func<StateContext, StateExpression[]> Builder)
{
    
}