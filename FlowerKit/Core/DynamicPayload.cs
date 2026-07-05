using System.Dynamic;
using System.Collections.Generic;

namespace FlowerKit.Core;

/// <summary>
/// Represetns a dynamic variable payload.
/// </summary>
public class DynamicPayload<T> : DynamicObject
    where T : Event
{
    public DynamicPayload(T source)
    {
        foreach (var prop in typeof(T).GetProperties())
            data.Add(prop.Name, prop.GetValue(source));
    }

    readonly Dictionary<string, object?> data = [];

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
        => data.TryGetValue(binder.Name, out result);
}