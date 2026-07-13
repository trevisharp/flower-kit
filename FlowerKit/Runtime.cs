using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FlowerKit;

using Core.Graph;
using Core.Executors;
using Core.Startup;

/// <summary>
/// The main runtime aplication.
/// </summary>
public static class Runtime
{
    public static IExecutor CurrentExecutor { get; set; } = new LocalExecutor();

    /// <summary>
    /// The event graph of the application, built at startup by <see cref="Run"/>.
    /// Used to wire the architecture (Executor, Kafka).
    /// </summary>
    public static FlowGraph? Graph { get; private set; }

    /// <summary>
    /// Publish a new event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Publish(object ev)
        => CurrentExecutor.Publish(ev);

    /// <summary>
    /// Start application.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run()
    {
        ArgumentNullException.ThrowIfNull(CurrentExecutor, nameof(CurrentExecutor));

        var codeAnalizer = new FlowCodeAnalyzer();
        Graph = codeAnalizer.Analize();
        
        InitWorkflows();

        CurrentExecutor.Run();
    }

    static void InitWorkflows()
    {
        var workflows = 
            from type in GetAllAssemblies().SelectMany(a => a.GetTypes())
            where type.IsClass
            where type.BaseType == typeof(Workflow)
            select type;

        foreach (var workflow in workflows)
        {
            var construtor = workflow.GetConstructor([]);
            if (construtor is null)
                continue;
            
            construtor.Invoke([]);
        }
    }
    
    static Assembly[] GetAllAssemblies()
    {
        var head = Assembly.GetEntryAssembly();
        if (head is null)
            return [];
        
        var loaded = new Dictionary<string, Assembly> {
            { head.GetName().FullName!, head }
        };
        VisitChildren(head);
        
        return [ ..loaded.Values ];

        void VisitChildren(Assembly assembly)
        {
            foreach (var reference in assembly.GetReferencedAssemblies())
                Visit(reference);
        }

        void Visit(AssemblyName assemblyName)
        {
            if (loaded.ContainsKey(assemblyName.FullName!))
                return;
            
            try
            {
                var assembly = Assembly.Load(assemblyName);
                loaded.Add(assembly.FullName!, assembly);
                VisitChildren(assembly);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"The assembly {assemblyName} cannot be loaded. Exception: {ex.Message}");
            }
        }
    }
}