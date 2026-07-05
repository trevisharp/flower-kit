using System;
using System.Reflection;
using System.Collections.Generic;

namespace FlowerKit.Core.Reload;

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

    public event Action<Assembly>? OnReload;

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
        
        foreach (var action in Actions)
        {
            if (action is null)
                continue;
            
            action();
        }
        
        var newAssembly = Compiler.Get();
        if (newAssembly is null)
            return;
        
        if (OnReload is not null)
            OnReload(newAssembly);
    }

    /// <summary>
    /// Force Reload wihtout verification.
    /// </summary>
    public void Reload()
    {
        foreach (var action in Actions)
        {
            if (action is null)
                continue;
            
            action();
        }
        
        var newAssembly = Compiler.Get();
        if (newAssembly is null)
            return;
        
        if (OnReload is not null)
            OnReload(newAssembly);
    }
}