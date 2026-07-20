using System;
using System.Linq.Expressions;

namespace FlowerKit;

using Core.StateExpressions;

/// <summary>
/// Extension methods for state expression
/// </summary>
public static class StateLinq
{
    public static StateExpression Select(
        this StateExpression exp, 
        Expression<Func<StateExpression, StateExpression>> selector
    )
    {
        return exp;
    }

    public static StateExpression Where(
        this StateExpression exp, 
        Expression<Func<object, bool>> selector
    )
    {
        return exp;
    }

    public static StateExpression Join(
        this StateExpression exp,
        Expression<Func<StateExpression, object>> left,
        Expression<Func<StateExpression, object>> rght,
        Expression<Func<StateExpression, StateExpression, StateExpression>> selector
    )
    {
        return exp;
    }
}