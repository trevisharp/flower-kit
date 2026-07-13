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
using Core.Testing;

/// <summary>
/// The main runtime aplication.
/// </summary>
public static class Runtime
{
    public static IExecutor CurrentExecutor { get; set; } = new LocalExecutor();

    public static TestRunner TestRunner { get; set; } = new TestRunner();

    /// <summary>
    /// The event graph of the application, built at startup by <see cref="Run"/>.
    /// Used to wire the architecture (Executor, Kafka).
    /// </summary>
    public static FlowGraph? Graph { get; private set; }

    /// <summary>
    /// The current environment (<see cref="Environments"/>). Resolved from the
    /// <c>FLOWERKIT_ENVIRONMENT</c> environment variable (falling back to
    /// <c>DOTNET_ENVIRONMENT</c>, then <see cref="Environments.Development"/>),
    /// but can be overridden in code before calling <see cref="Run"/>.
    /// </summary>
    public static string Environment { get; set; } =
        System.Environment.GetEnvironmentVariable("FLOWERKIT_ENVIRONMENT")
        ?? System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
        ?? Environments.Development;

    /// <summary>
    /// Every workflow instance found at startup, keyed by type name.
    /// </summary>
    public static IReadOnlyDictionary<string, Workflow> Workflows { get; private set; } =
        new Dictionary<string, Workflow>();

    static readonly List<Event> emittedTestEvents = [];

    /// <summary>
    /// Every event published so far, in order. Only recorded when
    /// <see cref="Environment"/> is <see cref="Environments.Test"/>, so
    /// <see cref="TestRunner"/> can evaluate assertions against it.
    /// </summary>
    public static IReadOnlyList<Event> EmittedTestEvents => emittedTestEvents;

    /// <summary>
    /// Publish a new event.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Publish(object ev)
    {
        if (Environment == Environments.Test && ev is Event e)
            emittedTestEvents.Add(e);

        CurrentExecutor.Publish(ev);
    }

    /// <summary>
    /// Clears the recorded <see cref="EmittedTestEvents"/>. Used by <see cref="TestRunner"/>
    /// to isolate the events of each test.
    /// </summary>
    static void ResetEmittedTestEvents()
        => emittedTestEvents.Clear();

    /// <summary>
    /// Start application.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(string[]? args)
    {
        ArgumentNullException.ThrowIfNull(CurrentExecutor, nameof(CurrentExecutor));
        ArgumentNullException.ThrowIfNull(TestRunner, nameof(TestRunner));

        ApplyConfigs(args ?? []);

        var codeAnalizer = new FlowCodeAnalyzer();
        Graph = codeAnalizer.Analize();

        InitWorkflows();

        CurrentExecutor.Run();

        if (Environment == Environments.Test)
            InitTests();
    }

    static void ApplyConfigs(string[] args)
    {
        foreach (var arg in args)
            ApplyConfig(arg);
    }

    static void ApplyConfig(string arg)
    {
        Environment = arg switch
        {
            "test" => Environments.Test,
            "stag" => Environments.Staging,
            "prod" => Environments.Production,
            _ => Environment
        };
    }

    /// <summary>
    /// Instantiate and register all workflows of the project.
    /// </summary>
    static void InitWorkflows()
    {
        var workflowTypes =
            from type in GetlAllTypes()
            where type.IsClass
            where type.BaseType == typeof(Workflow)
            select type;

        var workflows = new Dictionary<string, Workflow>();
        foreach (var type in workflowTypes)
        {
            var constructor = type.GetConstructor([]);
            if (constructor is null)
                continue;

            if (constructor.Invoke([]) is not Workflow workflow)
                continue;

            workflows[type.Name] = workflow;
        }

        Workflows = workflows;
    }

    /// <summary>
    /// Instantiate and register all tests of the project.
    /// </summary>
    static void InitTests()
    {
        var testTypes =
            from type in GetlAllTypes()
            where type.IsClass
            where type.BaseType == typeof(Test)
            select type;

        foreach (var type in testTypes)
        {
            var constructor = type.GetConstructor([]);
            if (constructor is null)
                continue;

            if (constructor.Invoke([]) is not Test test)
                continue;

            ResetEmittedTestEvents();
            TestRunner.RunTest(test);
        }
    }
    
    static IEnumerable<Type> GetlAllTypes()
        => GetAllAssemblies().SelectMany(GetLoadableTypes);

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
    
    static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}