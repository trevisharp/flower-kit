using System;
using System.Linq;
using System.Collections.Generic;

namespace FlowerKit.Core.Testing;

/// <summary>
/// Discovers every <see cref="Test"/>-derived record in the loaded assemblies,
/// runs its actions, and evaluates the assertion chains it registered against
/// the events <see cref="Runtime"/> recorded during the run.
/// </summary>
public class TestRunner
{
    /// <summary>
    /// Runs a single test: executes its actions (which register assertion
    /// chains and publish events), then evaluates every chain against the
    /// events recorded for this run.
    /// </summary>
    public virtual bool RunTest(Test test)
    {
        var chains = Test.BeginCollecting();
        try
        {
            foreach (var action in test.Actions)
                action();
        }
        finally
        {
            Test.EndCollecting();
        }

        var type = test.GetType();
        var emitted = Runtime.EmittedTestEvents;
        var passed = true;

        foreach (var chain in chains)
        {
            var reason = Evaluate(chain, emitted);
            if (reason is null)
                continue;

            passed = false;
            Log.Error($"FAIL {type.Name}: {reason}");
        }

        if (passed)
            Log.Info($"PASS {type.Name}");

        return passed;
    }

    /// <summary>
    /// Greedily matches each step of the chain, in order, to the first event
    /// after the previous match whose type and predicate agree. A step that
    /// finds no matching event fails the whole chain. Return the error reason
    /// or null in success case.
    /// </summary>
    protected virtual string? Evaluate(AssertionChain chain, IReadOnlyList<Event> emitted)
    {
        var cursor = 0;

        for (var i = 0; i < chain.Steps.Count; i++)
        {
            var step = chain.Steps[i];
            var found = false;

            for (var idx = cursor; idx < emitted.Count; idx++)
            {
                if (emitted[idx].GetType() != step.EventType)
                    continue;

                var snapshot = emitted.Take(idx + 1).ToList();
                if (!step.Predicate(snapshot))
                    continue;

                cursor = idx + 1;
                found = true;
                break;
            }

            if (found)
                continue;

            return $"step {i + 1} ({step.EventType.Name}): {step.Description} not satisfied";
        }

        return null;
    }
}
