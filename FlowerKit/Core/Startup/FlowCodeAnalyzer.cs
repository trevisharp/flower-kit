using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FlowerKit.Core.Startup;

/// <summary>
/// A object to analyzer flow implementations and generate insights.
/// </summary>
public class FlowCodeAnalyzer
{
    /// <summary>
    /// Analize the code of the current running project.
    /// </summary>
    public virtual void Analize()
    {
        var compiler = new AssemblyCompiler();
        var trees = compiler.GetSyntaxTrees();

        foreach (var tree in trees)
            AnalizeTree(tree);
    }

    protected virtual void AnalizeTree(SyntaxTree tree)
    {
        var root = tree.GetCompilationUnitRoot();

        var workflows = 
            from node in root.DescendantNodes()
            let info = TryParseWorkflow(node)
            where info is not null
            select info;
        
        Console.WriteLine("Project Structure:");
        foreach (var workflow in workflows)
        {
            Console.WriteLine(workflow.Name);
            foreach (var flow in workflow.Flows)
                Console.WriteLine($"\t{flow.Method} {flow.Type} -> {string.Join(", ", flow.Calls.Select(x => $"{x.Action} {x.To}"))}");
        }
    }

    /// <summary>
    /// Check if a syntax node is a workflow definition and return info.
    /// </summary>
    protected virtual WorkflowInfo? TryParseWorkflow(SyntaxNode node)
    {
        if (node is not RecordDeclarationSyntax rec)
            return null;
        
        var baseList = rec.BaseList?.ChildNodes() ?? [];
        foreach (var baseDef in baseList)
        {
            if (baseDef is not PrimaryConstructorBaseTypeSyntax baseCto)
                continue;
            
            var identifier = 
                baseCto.ChildNodes()
                .OfType<IdentifierNameSyntax>()
                .FirstOrDefault();
            if (identifier is null)
                continue;
            
            var typeName = identifier.Identifier.Text;
            if (typeName != nameof(Workflow))
                continue;
            
            var workflowName = rec.Identifier.Text;
            
            var argList =
                baseCto.ChildNodes()
                .OfType<ArgumentListSyntax>()
                .FirstOrDefault();
            if (argList is null)
                continue;
            
            return new WorkflowInfo(
                workflowName,
                [   
                    ..from arg in argList.ChildNodes().OfType<ArgumentSyntax>()
                    let flowInfo = TryParseFlowExpression(arg)
                    where flowInfo is not null
                    select flowInfo
                ]
            );
        }

        return null;
    }

    /// <summary>
    /// Process a syntax node and try parse a flow definition call.
    /// </summary>
    protected virtual FlowInfo? TryParseFlowExpression(ArgumentSyntax arg)
    {
        if (arg.Expression is not InvocationExpressionSyntax invocation)
            return null;

        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return null;

        var flowMethod = member.Name.Identifier.Text;

        var generic = (member.Name as GenericNameSyntax)?
            .TypeArgumentList.Arguments
            .FirstOrDefault()?
            .ToString();

        var lambda = invocation.ArgumentList.Arguments
            .Select(a => a.Expression)
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault();

        if (lambda is null)
            return null;

        var calls = ProcessLambda(lambda);

        return new FlowInfo(
            flowMethod,
            generic,
            [ ..calls ]
        );
    }
    
    private IEnumerable<PublishCallInfo> ProcessLambda(LambdaExpressionSyntax lambda)
    {
        var invokations = lambda.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();
        
        foreach (var invoke in invokations)
        {
            var invokeData = ProcessInvocation(invoke);
            if (invokeData is not null)
                yield return invokeData;
        }
    }

    private PublishCallInfo? ProcessInvocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return null;

        if (member.Expression is not GenericNameSyntax generic)
            return null;

        if (generic.Identifier.Text != nameof(Publish<>))
            return null;

        var target =
            generic.TypeArgumentList.Arguments
                .Single().ToString();

        var action = member.Name.Identifier.Text;

        return new (action, target);
    }

    static void PrintNode(SyntaxNode node, int level = 0)
    {
        Console.WriteLine($"{new string(' ', level * 2)}{node.Kind()}");

        foreach (var child in node.ChildNodes())
            PrintNode(child, level + 1);
    }

    public record WorkflowInfo(string Name, FlowInfo[] Flows);
    public record FlowInfo(string Method, string? Type, PublishCallInfo[] Calls);
    public record PublishCallInfo(string Action, string To);
}