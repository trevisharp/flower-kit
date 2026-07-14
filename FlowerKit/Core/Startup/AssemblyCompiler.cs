using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FlowerKit.Core.Startup;

/// <summary>
/// Compile and get a Assembly.
/// </summary>
public class AssemblyCompiler
{
    public string MainDirectory { get; set; } = Environment.CurrentDirectory;
    public List<Assembly> ExtraReferences { get; private set; } = [];

    /// <summary>
    /// Get a new assembly of compilation from files in 'MainDirectory'
    /// </summary>
    public virtual Assembly? Get()
        => Emit(GetCompilation());

    /// <summary>
    /// Get the syntax tree from files in 'MainDirectory'
    /// </summary>
    public virtual IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        var syntaxTrees = GetSyntaxTrees(
            MainDirectory
        );
        return syntaxTrees;
    }

    /// <summary>
    /// Get all .cs files in a directory and recursively.
    /// </summary>
    protected virtual IEnumerable<string> FindAllCSharpFiles(string directory)
    {
        var files = Directory.GetFiles(
            directory, "*.cs", 
            SearchOption.AllDirectories
        );
        
        var codeFiles = 
            from file in files
            where !file.Contains("\\bin\\")
            where !file.Contains("\\obj\\")
            where !file.Contains("/bin/")
            where !file.Contains("/obj/")
            select file;
        
        foreach (var file in codeFiles.Distinct())
            yield return file;
    }
    
    protected virtual IEnumerable<MetadataReference> GetReferences(IEnumerable<Assembly> extraRefs)
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblies = assembly!
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Append(assembly)
            .Append(Assembly.Load("System.Linq.Expressions"))
            .Append(Assembly.Load("System.Private.CoreLib"))
            // Required so code that uses 'dynamic' (e.g. Publish<T>.Emit) can be emitted.
            .Append(Assembly.Load("Microsoft.CSharp"))
            .Concat(extraRefs);
        
        return
            from a in assemblies
            select a.Location into loc
            select MetadataReference.CreateFromFile(loc);
    }

    /// <summary>
    /// Get a collection of syntax tree from cs files from a directory.
    /// </summary>
    public virtual IEnumerable<SyntaxTree> GetSyntaxTrees(string directory)
    {
        var files = FindAllCSharpFiles(directory);

        var syntaxTrees = files
            .Select(File.ReadAllText)
            .Select(text => CSharpSyntaxTree.ParseText(text));

        return syntaxTrees;
    }

    /// <summary>
    /// The SDK implicit global usings. MSBuild generates these into 'obj' when
    /// 'ImplicitUsings' is enabled, but we compile the raw source and skip 'obj',
    /// so we synthesize them to bind user code that relies on them (e.g. Console).
    /// System.Net.Http is intentionally omitted: it is a separate assembly that is
    /// not always referenced at runtime, and an unresolved using would turn into a
    /// compilation error that breaks the emit path.
    /// </summary>
    protected virtual SyntaxTree GetImplicitUsingsTree()
    {
        const string implicitUsings = """
            global using global::System;
            global using global::System.Collections.Generic;
            global using global::System.IO;
            global using global::System.Linq;
            global using global::System.Threading;
            global using global::System.Threading.Tasks;
            """;

        return CSharpSyntaxTree.ParseText(implicitUsings);
    }

    /// <summary>
    /// Build a compilation from files in 'MainDirectory'. The same compilation is
    /// reused both to emit an assembly (HotReload) and to run semantic analysis,
    /// so syntax trees and semantic models stay consistent.
    /// </summary>
    public virtual CSharpCompilation GetCompilation()
    {
        var syntaxTrees = GetSyntaxTrees(MainDirectory)
            .Prepend(GetImplicitUsingsTree());

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication
        );

        return CSharpCompilation.Create(
            "HotReloadAppend",
            syntaxTrees: syntaxTrees,
            references: GetReferences(ExtraReferences),
            options: compilationOptions
        );
    }

    /// <summary>
    /// Emits an assembly from an existing compilation (e.g. one already used for
    /// semantic analysis, so the source isn't parsed twice). On failure, prints
    /// the compilation's error diagnostics and returns null.
    /// </summary>
    public virtual Assembly? Emit(CSharpCompilation compilation)
    {
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (result.Success)
        {
            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
                Console.Error.WriteLine(diagnostic.ToString());
        }

        return null;
    }
}