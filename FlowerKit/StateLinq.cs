using System;
using System.Linq.Expressions;

namespace FlowerKit;

using Core.StateExpressions;

/// <summary>
/// Extension methods for state expression
/// </summary>
public static class StateLinq
{
    public static StateExpression<R> SelectMany<E, S, R>(
        this StateExpression<E> exp,
        Func<E, StateExpression<S>> pairing,
        Func<E, S, R> selector
    )
    {
        return null;
    }
    public static StateExpression<R> Select<T, R>(
        this StateExpression<T> exp, 
        Func<T, R> selector
    )
    {
        return null;
    }

    public static StateExpression<T> Where<T>(
        this StateExpression<T> exp, 
        Expression<Func<T, bool>> selector
    )
    {
        return exp;
    }

    public static StateExpression<R> Join<E, S, R>(
        this StateExpression<E> exp,
        StateExpression<S> other,
        Expression<Func<E, object>> left,
        Expression<Func<S, object>> rght,
        Func<E, S, R> selector
    )
    {
        return null;
    }
}