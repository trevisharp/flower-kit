using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FlowerKit.Core.Startup;

using Graph;

/// <summary>
/// Analyzes the source of the running project with the Roslyn semantic model and
/// builds a <see cref="FlowGraph"/>: which events exist, which events trigger which
/// flows, and which events each flow can publish. Publish calls are followed in
/// depth through the user's own methods; third-party code (no source) is ignored.
/// </summary>
public class FlowCodeAnalyzer
{
    const string EventMetadataName = "FlowerKit.Event";
    const string FlowMetadataName = "FlowerKit.Flow";
    const string PublishMetadataName = "FlowerKit.Publish`1";
    const string WorkflowMetadataName = "FlowerKit.Workflow";

    /// <summary>
    /// The <see cref="Flow"/> factory methods that define a flow. Adding a new one
    /// (a new way to declare a flow) is a single entry here.
    /// </summary>
    protected virtual IReadOnlyList<FlowAnchor> Anchors { get; } =
    [
        new("On", IsTrigger: true),
        new("New", IsTrigger: false)
    ];

    /// <summary>
    /// Analyze the code of the current running project and return its flow graph,
    /// or null when the project cannot be compiled.
    /// </summary>
    public virtual FlowGraph? Analize()
    {
        var compiler = new AssemblyCompiler();
        var compilation = compiler.GetCompilation();
        return Analize(compilation);
    }

    /// <summary>
    /// Analyze an already-built compilation and return its flow graph. Used on
    /// HotReload to reuse the compilation that was also used to emit the new
    /// assembly, instead of parsing the source a second time.
    /// </summary>
    public virtual FlowGraph? Analize(Compilation compilation)
    {
        var events = CollectEvents(compilation);
        var workflows = CollectWorkflows(compilation);
        var flows = CollectFlows(compilation);

        var graph = new FlowGraph(events, workflows, flows);
        PrintGraph(graph);
        return graph;
    }

    /// <summary>
    /// Collect every user-defined type that inherits from <see cref="Event"/>.
    /// </summary>
    protected virtual IReadOnlyCollection<string> CollectEvents(Compilation compilation)
    {
        var eventSymbol = compilation.GetTypeByMetadataName(EventMetadataName);
        var events = new List<string>();
        if (eventSymbol is null)
            return events;

        foreach (var type in GetSourceTypes(compilation.GlobalNamespace))
        {
            if (InheritsFrom(type, eventSymbol))
                events.Add(type.Name);
        }

        return events;
    }

    /// <summary>
    /// Collect every user-defined type that inherits from <see cref="Workflow"/>.
    /// </summary>
    protected virtual IReadOnlyCollection<string> CollectWorkflows(Compilation compilation)
    {
        var workflowSymbol = compilation.GetTypeByMetadataName(WorkflowMetadataName);
        var workflows = new List<string>();
        if (workflowSymbol is null)
            return workflows;

        foreach (var type in GetSourceTypes(compilation.GlobalNamespace))
        {
            if (InheritsFrom(type, workflowSymbol))
                workflows.Add(type.Name);
        }

        return workflows;
    }

    /// <summary>
    /// Find every flow definition (a call to an anchor method on <see cref="Flow"/>)
    /// and resolve the events it may publish, in depth.
    /// </summary>
    protected virtual IReadOnlyCollection<FlowNode> CollectFlows(Compilation compilation)
    {
        var flowSymbol = compilation.GetTypeByMetadataName(FlowMetadataName);
        var publishSymbol = compilation.GetTypeByMetadataName(PublishMetadataName);
        var workflowSymbol = compilation.GetTypeByMetadataName(WorkflowMetadataName);
        var nodes = new List<FlowNode>();
        if (flowSymbol is null)
            return nodes;

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);

            var invocations = tree.GetRoot()
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var node = TryParseFlow(invocation, model, compilation, flowSymbol, publishSymbol, workflowSymbol);
                if (node is not null)
                    nodes.Add(node);
            }
        }

        return nodes;
    }

    /// <summary>
    /// Try to read an invocation as a flow definition (e.g. <c>Flow.On&lt;T&gt;(...)</c>),
    /// returning its node with the trigger event, every publishable event, and the
    /// enclosing <see cref="Workflow"/> type (if any).
    /// </summary>
    protected virtual FlowNode? TryParseFlow(
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        Compilation compilation,
        INamedTypeSymbol flowSymbol,
        INamedTypeSymbol? publishSymbol,
        INamedTypeSymbol? workflowSymbol
    )
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
            return null;

        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, flowSymbol))
            return null;

        var anchor = Anchors.FirstOrDefault(a => a.Method == method.Name);
        if (anchor is null)
            return null;

        string? trigger = null;
        if (anchor.IsTrigger && method.TypeArguments.Length > 0)
            trigger = method.TypeArguments[0].Name;

        var publishes = new HashSet<PublishEdge>();
        var delegateArg = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        if (delegateArg is not null)
        {
            var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            CollectFromDelegate(delegateArg, model, compilation, publishSymbol, publishes, visited);
        }

        var workflow = GetEnclosingWorkflow(invocation, model, workflowSymbol);

        return new FlowNode(anchor, trigger, [.. publishes], workflow);
    }

    /// <summary>
    /// Finds the name of the type declaration enclosing an invocation when that
    /// type inherits from <see cref="Workflow"/> (e.g. a flow passed to the base
    /// constructor of a workflow record).
    /// </summary>
    protected virtual string? GetEnclosingWorkflow(
        SyntaxNode node,
        SemanticModel model,
        INamedTypeSymbol? workflowSymbol
    )
    {
        if (workflowSymbol is null)
            return null;

        var typeDecl = node.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();
        if (typeDecl is null)
            return null;

        if (model.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol type)
            return null;

        return InheritsFrom(type, workflowSymbol) ? type.Name : null;
    }

    /// <summary>
    /// Start the depth-first publish search from the delegate passed to an anchor:
    /// a lambda body, or a referenced method / local function (method group).
    /// </summary>
    protected virtual void CollectFromDelegate(
        ExpressionSyntax delegateArg,
        SemanticModel model,
        Compilation compilation,
        INamedTypeSymbol? publishSymbol,
        HashSet<PublishEdge> publishes,
        HashSet<ISymbol> visited
    )
    {
        if (delegateArg is LambdaExpressionSyntax lambda && lambda.Body is not null)
        {
            CollectFromBody(lambda.Body, compilation, publishSymbol, publishes, visited);
            return;
        }

        if (model.GetSymbolInfo(delegateArg).Symbol is IMethodSymbol method)
            CollectFromMethod(method, compilation, publishSymbol, publishes, visited);
    }

    /// <summary>
    /// Walk every invocation inside a body: record <c>Publish&lt;X&gt;.Action(...)</c>
    /// calls, and recurse into calls to the user's own methods.
    /// </summary>
    protected virtual void CollectFromBody(
        SyntaxNode body,
        Compilation compilation,
        INamedTypeSymbol? publishSymbol,
        HashSet<PublishEdge> publishes,
        HashSet<ISymbol> visited
    )
    {
        var model = compilation.GetSemanticModel(body.SyntaxTree);

        var invocations = body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (TryParsePublish(invocation, model, publishSymbol, out var edge))
            {
                publishes.Add(edge);
                continue;
            }

            if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol callee)
                CollectFromMethod(callee, compilation, publishSymbol, publishes, visited);
        }
    }

    /// <summary>
    /// Recurse into a user-defined method's body. Methods without source (third
    /// party) are ignored, and a visited set breaks recursion cycles.
    /// </summary>
    protected virtual void CollectFromMethod(
        IMethodSymbol method,
        Compilation compilation,
        INamedTypeSymbol? publishSymbol,
        HashSet<PublishEdge> publishes,
        HashSet<ISymbol> visited
    )
    {
        var definition = method.OriginalDefinition;
        if (definition.DeclaringSyntaxReferences.Length == 0)
            return;

        if (!visited.Add(definition))
            return;

        foreach (var reference in definition.DeclaringSyntaxReferences)
        {
            var body = GetBody(reference.GetSyntax());
            if (body is not null)
                CollectFromBody(body, compilation, publishSymbol, publishes, visited);
        }
    }

    /// <summary>
    /// Try to read an invocation as <c>Publish&lt;X&gt;.Action(...)</c>. It matches on
    /// the receiver being the <see cref="Publish{T}"/> type, so it works even though
    /// the publish member is <c>dynamic</c>, and captures the member name as the action.
    /// </summary>
    protected virtual bool TryParsePublish(
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        INamedTypeSymbol? publishSymbol,
        out PublishEdge edge
    )
    {
        edge = default;
        if (publishSymbol is null)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;

        if (model.GetSymbolInfo(member.Expression).Symbol is not INamedTypeSymbol receiver)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(receiver.OriginalDefinition, publishSymbol))
            return false;

        var eventType = receiver.TypeArguments.Length > 0
            ? receiver.TypeArguments[0].Name
            : "?";
        var action = member.Name.Identifier.Text;

        edge = new PublishEdge(eventType, action);
        return true;
    }

    /// <summary>
    /// Gets the executable body of a method or local function declaration.
    /// </summary>
    protected static SyntaxNode? GetBody(SyntaxNode declaration) => declaration switch
    {
        MethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax l => (SyntaxNode?)l.Body ?? l.ExpressionBody?.Expression,
        _ => null
    };

    /// <summary>
    /// Enumerates all types declared in source under a namespace.
    /// </summary>
    protected static IEnumerable<INamedTypeSymbol> GetSourceTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.Locations.Any(l => l.IsInSource))
                yield return type;

            foreach (var nested in type.GetTypeMembers())
                if (nested.Locations.Any(l => l.IsInSource))
                    yield return nested;
        }

        foreach (var child in ns.GetNamespaceMembers())
            foreach (var type in GetSourceTypes(child))
                yield return type;
    }

    /// <summary>
    /// Whether <paramref name="type"/> derives from <paramref name="baseType"/>.
    /// </summary>
    protected static bool InheritsFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

        return false;
    }

    /// <summary>
    /// Prints the graph for inspection.
    /// </summary>
    protected virtual void PrintGraph(FlowGraph graph)
    {
        Console.WriteLine("FlowGraph:");
        Console.WriteLine($"  Events: {string.Join(", ", graph.Events)}");
        Console.WriteLine($"  Workflows: {string.Join(", ", graph.Workflows)}");

        foreach (var flow in graph.Flows)
        {
            var trigger = flow.TriggerEvent ?? "(no trigger)";
            var publishes = flow.Publishes.Count == 0
                ? "-"
                : string.Join(", ", flow.Publishes);
            var workflow = flow.Workflow ?? "(standalone)";
            Console.WriteLine($"  [{workflow}] [{flow.Anchor.Method}] {trigger} -> {publishes}");
        }
    }
}
