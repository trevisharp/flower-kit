using System;
using System.Linq;
using System.Dynamic;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace FlowerKit.Internal;

/// <summary>
/// A publisher to handle Event.Publish calls.
/// </summary>
public class Publisher<T> : DynamicObject
    where T : Event<T>
{
    static readonly Type Type;
    static readonly ConstructorInfo Constructor;
    static readonly ParameterInfo[] CtorParameters;
    static readonly ConcurrentDictionary<int, Func<object?[], T>> FactoryMap = [];

    static Publisher()
    {
        Type = typeof(T);
        
        Constructor = Type
            .GetConstructors()
            .FirstOrDefault() 
            ?? throw new Exception($"The type {Type} may contains a public constructor.");

        CtorParameters = Constructor.GetParameters();
    }

    /// <summary>
    /// Publish a event on system.
    /// </summary>
    void Publish(T ev)
    {
        System.Console.WriteLine(ev);
    }

    public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
    {
        if (args is null)
            throw new Exception($"The publish of {Type} may receive values."); 

        var factory = GetPublishFactory(binder.CallInfo.ArgumentNames);
        var eventPayload = factory(args);
        Publish(eventPayload);

        result = null;
        return true;
    }

    /// <summary>
    /// Get a cached factory for publish payload or create a newer if
    /// the args sequence dont match.
    static Func<object?[], T> GetPublishFactory(ReadOnlyCollection<string> args)
    {
        var hash = GetSignatureHash(args);
        if (FactoryMap.TryGetValue(hash, out var factory))
            return factory;

        var newFactory = CreateNewFactory(Constructor, args);
        if (!FactoryMap.TryAdd(hash, newFactory))
            throw new Exception("Signature conflict. Try change order of parameters on call to fix.");
        
        return newFactory;
    }

    /// <summary>
    /// Generate a hash from a named call of functions.
    /// </summary>
    static int GetSignatureHash(ReadOnlyCollection<string> args)
    {
        var hash = new HashCode();

        foreach (var name in args)
            hash.Add(name);

        return hash.ToHashCode();
    }

    /// <summary>
    /// Create a new factory based on args order and a constructor.
    /// </summary>
    static Func<object?[], T> CreateNewFactory(
        ConstructorInfo ctor,
        ReadOnlyCollection<string> args
    )
    {
        int[] indexMap = new int[CtorParameters.Length];

        Dictionary<string, int> callMap = [];
        for (int i = 0; i < args.Count; i++)
            callMap.Add(args[i], i);
        
        int k = 0;
        foreach (var param in CtorParameters)
        {
            if (!callMap.TryGetValue(param.Name!, out var index))
                throw new Exception($"Missing value of '{param.Name}' on '{Type}' publishing call.");
            
            indexMap[k++] = index;
        }

        return CompileFactory(ctor, indexMap);
    }

    /// <summary>
    /// Compile a reflection call to call constructor faster.
    /// </summary>
    static Func<object?[], T> CompileFactory(ConstructorInfo ctor, int[] indexMap)
    {
        var args = Expression.Parameter(typeof(object[]), "args");

        var ctorArgs = CtorParameters
            .Zip(indexMap)
            .Select(pair =>
                Expression.Convert(
                    Expression.ArrayIndex(args, Expression.Constant(pair.Second)),
                    pair.First.ParameterType
                )
            ).ToArray();

        var body = Expression.New(ctor, ctorArgs);

        return Expression
            .Lambda<Func<object?[], T>>(body, args)
            .Compile();
    }
}