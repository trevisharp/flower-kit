using System;
using System.Linq;
using System.Runtime.InteropServices;
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

        Console.WriteLine("Syntax Tree:");
        PrintNode(root);
        
        foreach (var node in root.DescendantNodes())
            AnalizeNode(node);
    }

    protected virtual void AnalizeNode(SyntaxNode node)
    {
        if (node is not RecordDeclarationSyntax rec)
            return;
        
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
            
            var argList =
                baseCto.ChildNodes()
                .OfType<ArgumentListSyntax>()
                .FirstOrDefault();
            if (argList is null)
                continue;
            
            var args = 
                argList.ChildNodes()
                .OfType<ArgumentSyntax>();
            
            foreach (var arg in args)
                ProcessFlowExpression(arg);
        }
    }

    protected virtual void ProcessFlowExpression(ArgumentSyntax arg)
    {
        
    }

    static void PrintNode(SyntaxNode node, int level = 0)
    {
        Console.WriteLine($"{new string(' ', level * 2)}{node.Kind()}");

        foreach (var child in node.ChildNodes())
            PrintNode(child, level + 1);
    }
}