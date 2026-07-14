using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace FlowerKit.Core.Startup;

/// <summary>
/// A Assembly reloader that get a new assembly when files changed.
/// </summary>
public class Reloader
{
    public required FileWatcher Watcher { get; set; }
    public required AssemblyCompiler Compiler { get; set; }

    /// <summary>
    /// Actions to run when files are modifieds.
    /// </summary>
    public List<Action> Actions { get; private set; } = [];

    /// <summary>
    /// Get the default configuration of Reloader class.
    /// </summary>
    public static Reloader GetDefault()
    {
        return new Reloader
        {
            Compiler = new AssemblyCompiler(),
            Watcher = new FileWatcher()
            .AddCSharpFilter()
        };
    }

    /// <summary>
    /// Raised with the freshly emitted assembly and the compilation that produced
    /// it, so a subscriber (e.g. <see cref="Runtime"/>) can reuse the compilation
    /// for semantic analysis instead of parsing the source a second time.
    /// </summary>
    public event Action<Assembly, CSharpCompilation>? OnReload;

    /// <summary>
    /// Try Reload code calling OnReload on success operation.
    /// </summary>
    public void TryReload()
    {
        if (OnReload is null)
            return;
        if (Watcher is null)
            return;
        if (Compiler is null)
            return;

        var verify = Watcher.Verify();
        if (!verify)
            return;

        Reload();
    }

    /// <summary>
    /// Force Reload wihtout verification. On a compilation error, the
    /// diagnostics are printed by <see cref="AssemblyCompiler.Emit"/> and
    /// <see cref="OnReload"/> is not raised, leaving the previous generation
    /// running.
    /// </summary>
    public void Reload()
    {
        foreach (var action in Actions)
        {
            if (action is null)
                continue;

            action();
        }

        var compilation = Compiler.GetCompilation();
        var newAssembly = Compiler.Emit(compilation);
        if (newAssembly is null)
            return;

        OnReload?.Invoke(newAssembly, compilation);
    }
}