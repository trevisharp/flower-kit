using System;
using System.Linq;
using System.Buffers;
using System.Dynamic;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace FlowerKit.Core;

using ConstructorFunc = Func<object?[], object>;

/// <summary>
/// A publisher to handle Event.Publish calls.
/// </summary>
public class Publisher : DynamicObject
{
    private static readonly ConcurrentDictionary<Type, Publisher> publisherMap = new();

    /// <summary>
    /// Get a publisher for events of a specified type.
    /// </summary>
    public static Publisher Get(Type type)
    {
        if (publisherMap.TryGetValue(type, out var pub))
            return pub;

        pub = new Publisher(type);
        publisherMap.TryAdd(type, pub);
        return pub;
    }

    readonly Type Type;
    readonly ConstructorInfo Constructor;
    readonly ParameterInfo[] CtorParameters;
    readonly ConcurrentDictionary<int, ConstructorFunc> FactoryMap = [];

    public Publisher(Type pubType)
    {
        Type = pubType;

        Constructor = pubType
            .GetConstructors()
            .FirstOrDefault()
            ?? throw new Exception($"The type {pubType} may contains a public constructor.");

        CtorParameters = Constructor.GetParameters();
    }
    
    public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
    {
        if (args is null)
            throw new Exception($"The publish of {Type} may receive values.");

        var factory = GetPublishFactory(binder.CallInfo.ArgumentNames);
        var eventPayload = factory(args);
        Planner.ReceiveEvent(eventPayload);

        result = null;
        return true;
    }
    
    /// <summary>
    /// Get a cached factory for publish payload or create a newer if
    /// the args sequence dont match.
    ConstructorFunc GetPublishFactory(ReadOnlyCollection<string> args)
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
    /// Create a new factory based on args order and a constructor.
    /// </summary>
    ConstructorFunc CreateNewFactory(
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
    ConstructorFunc CompileFactory(ConstructorInfo ctor, int[] indexMap)
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
            .Lambda<ConstructorFunc>(body, args)
            .Compile();
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
}