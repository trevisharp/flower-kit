using FlowerKit;

Runtime.Run();

Publish<CollatzOdd>.Emit(
    Start: 7,
    Value: 7
);

record CollatzOdd(
    int Start,
    int Value
) : Event;

record CollatzEven(
    int Start,
    int Value
) : Event;

record CollatzEnd : Event;

record CollatzWorkflow() : Workflow(
    Flow.On<CollatzOdd>(ctx =>
    {
        Console.WriteLine($"Receive number {ctx.Data}");
        Publish<CollatzEven>.Emit(
            ~ctx.Data,
            Value: 3 * ctx.Data.Value + 1
        );
    }),

    Flow.On<CollatzEven>(ctx =>
    {
        Console.WriteLine($"Receive number {ctx.Data}");
        var value = ctx.Data.Value;
        while (value % 2 == 0)
            value /= 2;
        
        if (value == 1)
            Publish<CollatzEnd>.Emit();
        else
            Publish<CollatzOdd>.Emit(
                ~ctx.Data,
                Value: value
            );
    }),

    Flow.On<CollatzEnd>(ctx =>
    {
        Console.WriteLine("Conjectura valida para um numero");
    })
);