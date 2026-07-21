using System;

namespace FlowerKit.Core.StateExpressions;

/// <summary>
/// Represents a possible action for state expressions.
/// </summary>
public class StateContext
{
    public StateExpression<T> Events<T>() 
        where T : Event => new(this);

    public StateExpression<S> States<S>() 
        where S : State => new(this);

    public StateExpression Delete(State e)
    {
        return new StateExpression(this);
    }
}