using System;
using System.Collections.Generic;

namespace FlowerKit.Core.Testing;

/// <summary>
/// An ordered sequence of assertion steps built by <c>Assert&lt;T&gt;(...).Then&lt;U&gt;(...)</c>.
/// Each step names the event type it must match and a predicate evaluated
/// against every event emitted so far, from the start of the test up to and
/// including the matched candidate.
/// </summary>
public sealed class AssertionChain
{
    readonly List<Step> steps = [];

    public IReadOnlyList<Step> Steps => steps;

    public void AddStep(Type eventType, Func<IReadOnlyList<Event>, bool> predicate, string description)
        => steps.Add(new Step(eventType, predicate, description));

    public sealed record Step(Type EventType, Func<IReadOnlyList<Event>, bool> Predicate, string Description);
}