#pragma warning disable IDE1006

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection.Metadata;

namespace FlowerKit;

public static class Kit
{
    public static void Publish<T>(T eventData)
    {
        
    }

    public static dynamic ev { get; set; } = new EventDefinition();
}