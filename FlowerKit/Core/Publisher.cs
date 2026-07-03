using System;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using FlowerKit.Core.EventOperators;
using System.Diagnostics;

namespace FlowerKit.Core;

using ConstructorFunc = Func<object?[], object>;

/// <summary>
/// A publisher to handle Publish<Event>.Emit calls.
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
        
        var arguments = ProcessSpecialEvents(binder, args);
        var factory = GetPublishFactory(arguments, args);
        var eventPayload = factory(args);
        Planner.ReceiveEvent(eventPayload);

        result = null;
        return true;
    }

    static ReadOnlyCollection<string> ProcessSpecialEvents(InvokeBinder binder, object?[] args)
    {        
        var unnamedArgs = 
            binder.CallInfo.ArgumentCount - binder.CallInfo.ArgumentNames.Count;
        if (unnamedArgs == 0)
            return binder.CallInfo.ArgumentNames;
        
        return [
            ..args.Take(unnamedArgs).SelectMany((arg, i) => arg switch {
                SpreadEvent spread => TakeSpreadFields(spread, i),
                null => throw new Exception($"Invalid unnamed null arg on publishing."),
                _ => throw new Exception($"Invalid unnamed arg type {arg.GetType()} on publishing.")
            }),
            ..binder.CallInfo.ArgumentNames
        ];

        static IEnumerable<string> TakeSpreadFields(SpreadEvent spread, int index)
        {
            var props = spread.Event.GetType().GetProperties();
            foreach (var prop in props)
                yield return $"~{index}~{prop.Name}";
        }
    }
    
    /// <summary>
    /// Get a cached factory for publish payload or create a newer if
    /// the args sequence dont match.
    ConstructorFunc GetPublishFactory(ReadOnlyCollection<string> args, object?[] values)
    {
        var hash = GetSignatureHash(args);
        if (FactoryMap.TryGetValue(hash, out var factory))
            return factory;

        var newFactory = CompilerNewFactory(Constructor, args, values);
        if (!FactoryMap.TryAdd(hash, newFactory))
            throw new Exception("Signature conflict. Try change order of parameters on call to fix.");

        return newFactory;
    }
    
    /// <summary>
    /// Create a new factory based on args order and a constructor.
    /// </summary>
    ConstructorFunc CompilerNewFactory(
        ConstructorInfo ctor,
        ReadOnlyCollection<string> args,
        object?[] values
    )
    {
        int i = 0;
        Dictionary<string, int> namedMap = [];
        foreach (var arg in args)
        {
            if (arg.StartsWith('~'))
            {
                var parts = arg.Split('~', StringSplitOptions.RemoveEmptyEntries);
                var name = $"~{parts[1]}";
                var index = int.Parse(parts[0]);
                namedMap[name] = index;
                i = index + 1;
                continue;
            }
            
            namedMap[arg] = i++;
        }
        
        var argsParam = Expression.Parameter(typeof(object[]), "args");
        var ctorArgs = new List<Expression>();

        foreach (var param in CtorParameters)
        {
            if (param.Name is null)
                throw new Exception($"Missing name for a parameter on type '{Type}'");
            
            if (namedMap.TryGetValue(param.Name, out var index))
            {
                ctorArgs.Add(FromIndex(param, index));
                continue;
            }

            if (namedMap.TryGetValue("~" + param.Name, out var spreadIndex))
            {
                ctorArgs.Add(FromSpread(param, spreadIndex));
                continue;
            }
        }

        var body = Expression.New(ctor, ctorArgs);
        return Expression
            .Lambda<ConstructorFunc>(body, argsParam)
            .Compile();
        
        Expression FromIndex(ParameterInfo param, int index)
        {
            return Expression.Convert(
                Expression.ArrayIndex(argsParam, Expression.Constant(index)),
                param.ParameterType
            );
        }

        Expression FromSpread(ParameterInfo param, int index)
        {
            var spread = (SpreadEvent)values[index]!;
            return Expression.Property(
                Expression.Convert(
                    Expression.Field(
                        Expression.Convert(
                            Expression.ArrayIndex(argsParam, Expression.Constant(index)),
                            typeof(SpreadEvent)
                        ),
                        "Event"
                    ),
                    spread.Event.GetType()
                ),
                param.Name!
            );
        }
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