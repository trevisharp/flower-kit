namespace FlowerKit.Core.StateExpressions;

/// <summary>
/// Represents a current state of a State Expression.
/// </summary>
public class StateExpression(StateContext context)
{
    protected readonly StateContext Context = context;
}

/// <summary>
/// Represents a current state of a State Expression.
/// </summary>
public class StateExpression<T>(StateContext context) : StateExpression(context)
{
    
}