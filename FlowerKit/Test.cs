using System;
using System.Collections.Generic;

namespace FlowerKit;

using Core.Testing;

/// <summary>
/// Declares an integration test. <paramref name="Actions"/> are run in order by
/// <see cref="TestRunner"/> when <see cref="Runtime.Environment"/> is
/// <see cref="Environments.Test"/>: typically an action registers a chain of
/// assertions via <see cref="Assert{T}"/> and then publishes the event that
/// should trigger the cascade being tested.
/// </summary>
public record Test(params IEnumerable<Action> Actions)
{
    [ThreadStatic]
    static List<AssertionChain>? activeChains;

    /// <summary>
    /// Starts collecting every <see cref="AssertionChain"/> built by
    /// <see cref="Assert{T}"/> while a single test's actions run.
    /// </summary>
    internal static List<AssertionChain> BeginCollecting()
    {
        var chains = new List<AssertionChain>();
        activeChains = chains;
        return chains;
    }

    /// <summary>Stops collecting chains started by <see cref="BeginCollecting"/>.</summary>
    internal static void EndCollecting()
        => activeChains = null;

    /// <summary>
    /// Starts an assertion chain: some event of type <typeparamref name="T"/> must
    /// be emitted at some point during the test, optionally matching
    /// <paramref name="condition"/>. Chain further steps with
    /// <see cref="TestState{T}.Then{U}"/>.
    /// </summary>
    protected static TestState<T> Assert<T>(Func<TestState<T>, bool>? condition = null)
        where T : Event
    {
        var chain = new AssertionChain();
        var state = new TestState<T>(chain);
        state.AddStep(condition, $"Assert<{typeof(T).Name}>");
        activeChains?.Add(chain);
        return state;
    }

    /// <summary>
    /// Both the fluent builder for an assertion chain and the snapshot handed to a
    /// step's predicate: <see cref="Events"/> holds every event emitted from the
    /// start of the test up to and including <see cref="Last"/>.
    /// </summary>
    public sealed class TestState<T>
        where T : Event
    {
        readonly AssertionChain chain;

        internal TestState(AssertionChain chain)
            => this.chain = chain;

        public IReadOnlyList<Event> Events { get; private init; } = [];
        public T Last { get; private init; } = default!;

        internal void AddStep(Func<TestState<T>, bool>? condition, string description)
            => chain.AddStep(typeof(T), events => condition?.Invoke(Snapshot(events)) ?? true, description);

        static TestState<T> Snapshot(IReadOnlyList<Event> events)
            => new(null!)
            {
                Events = events,
                Last = (T)events[^1]
            };

        /// <summary>
        /// Appends the next step: some event of type <typeparamref name="E"/> must
        /// be emitted after the previous step's match, optionally matching
        /// <paramref name="condition"/>.
        /// </summary>
        public TestState<E> Then<E>(Func<TestState<E>, bool>? condition = null)
            where E : Event
        {
            var next = new TestState<E>(chain);
            next.AddStep(condition, $"Then<{typeof(E).Name}>");
            return next;
        }
    }
}