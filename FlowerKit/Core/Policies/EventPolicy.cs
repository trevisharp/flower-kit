using System;

namespace FlowerKit.Core.Policies;

/// <summary>
/// A policy claimed by a event.
/// </summary>
public record EventPolicy(Policy Policy, Type EventType);